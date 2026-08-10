import json
import os
from io import BytesIO
from pathlib import Path
from threading import Lock
from typing import Final

import cv2
import mediapipe as mp
import numpy as np
import torch
import torch.nn.functional as functional
from fastapi import (
    FastAPI,
    File,
    Form,
    HTTPException,
    UploadFile,
)
from fastapi.middleware.cors import CORSMiddleware
from fastapi.responses import Response
from mobile_sam import SamPredictor, sam_model_registry
from PIL import (
    Image,
    ImageFilter,
    ImageOps,
    UnidentifiedImageError,
)
from pydantic import BaseModel
from transformers import (
    AutoImageProcessor,
    AutoModelForSemanticSegmentation,
)

MAX_IMAGE_SIZE_BYTES: Final = 6 * 1024 * 1024
MAX_IMAGE_PIXELS: Final = 20_000_000
INFERENCE_DEVICE: Final = (
    "cuda" if torch.cuda.is_available() else "cpu"
)

ALLOWED_CONTENT_TYPES: Final = {
    "image/jpeg",
    "image/png",
    "image/webp",
}

CLOTHING_MODEL_NAME: Final = (
    "mattmdjaga/segformer_b2_clothes"
)

MOBILE_SAM_CHECKPOINT: Final = Path(
    os.getenv(
        "MOBILE_SAM_CHECKPOINT",
        str(Path(__file__).resolve().parents[1] / "models" / "mobile_sam.pt"),
    )
)

GARMENT_LABELS: Final = {
    "upper-clothes": {4},
    "skirt": {5},
    "pants": {6},
    # Use only the dedicated dress class. Combining upper-clothes and skirt
    # labels can incorrectly include trees, floors, furniture, and walls.
    "dress": {7},
    "shoes": {9, 10},
    "bag": {16},
    "scarf": {17},
}
ITEM_TYPE_ALIASES: Final = {
    "accessory": "bag",
    "accessories": "bag",
    "bags": "bag",
    "shoe": "shoes",
    "sneaker": "shoes",
    "sneakers": "shoes",
    "trouser": "pants",
    "trousers": "pants",
    "bottom": "pants",
    "bottoms": "pants",
    "shirt": "upper-clothes",
    "shirts": "upper-clothes",
    "top": "upper-clothes",
    "tops": "upper-clothes",
}

RECOLOR_CONTRAST: Final = {
    "upper-clothes": 0.85,
    "dress": 0.85,
    "skirt": 0.82,
    "pants": 0.82,
    "shoes": 0.72,
    "bag": 0.78,
    "scarf": 0.80,
    "standalone-product": 0.80,
}

POSE_REQUIREMENTS: Final = {
    "fullBodyPhotoUrl": {
        "head": (0,),
        "shoulders": (11, 12),
        "hips": (23, 24),
        "knees": (25, 26),
        "ankles": (27, 28),
    },
    "upperBodyPhotoUrl": {
        "head": (0,),
        "shoulders": (11, 12),
        "hips": (23, 24),
    },
    "lowerBodyPhotoUrl": {
        "hips": (23, 24),
        "knees": (25, 26),
        "ankles": (27, 28),
    },
}


class ValidationResponse(BaseModel):
    accepted: bool
    message: str
    slot: str
    checks: dict[str, object]


app = FastAPI(
    title="Virtual Try-On AI Service",
    version="2.1.0",
)

allowed_origins = [
    origin.strip()
    for origin in os.getenv(
        "ALLOWED_ORIGINS",
        (
            "http://localhost:5173,"
            "http://127.0.0.1:5173"
        ),
    ).split(",")
    if origin.strip()
]

app.add_middleware(
    CORSMiddleware,
    allow_origins=allowed_origins,
    allow_credentials=False,
    allow_methods=["GET", "POST"],
    allow_headers=["Content-Type"],
    expose_headers=[
        "X-Detected-Item-Type",
        "X-Target-Color",
        "X-Segmentation-Mode",
        "X-Recolor-Engine",
        "X-Mask-Score",
        "X-Image-Width",
        "X-Image-Height",
    ],
)


# MediaPipe models validate profile and try-on photos.
face_detector = (
    mp.solutions.face_detection.FaceDetection(
        model_selection=0,
        min_detection_confidence=0.6,
    )
)

pose_detector = mp.solutions.pose.Pose(
    static_image_mode=True,
    model_complexity=1,
    enable_segmentation=False,
    min_detection_confidence=0.6,
)

detector_lock = Lock()


# Models are loaded lazily when first used.
clothing_model_lock = Lock()
clothing_processor = None
clothing_model = None
mobile_sam_lock = Lock()
mobile_sam_predictor = None


@app.get("/health")
def health() -> dict[str, str]:
    return {
        "status": "ok",
        "validationEngine": "MediaPipe",
        "segmentationEngine": "MobileSAM",
        "recolorEngine": "Approved-mask local recoloring",
        "inferenceDevice": INFERENCE_DEVICE,
    }


@app.post(
    "/validate-image",
    response_model=ValidationResponse,
)
def validate_image(
    slot: str = Form(...),
    image: UploadFile = File(...),
) -> ValidationResponse:
    normalized_slot = slot.strip()

    supported_slots = {
        "profilePhotoUrl",
        "facePhotoUrl",
        *POSE_REQUIREMENTS,
    }

    if normalized_slot not in supported_slots:
        raise HTTPException(
            status_code=400,
            detail="Unsupported image slot.",
        )

    image_bytes = read_uploaded_image(image)
    rgb_image = read_rgb_image(image_bytes)

    with detector_lock:
        face_results = face_detector.process(
            rgb_image
        )

        pose_results = (
            pose_detector.process(rgb_image)
            if normalized_slot in POSE_REQUIREMENTS
            else None
        )

    if normalized_slot in {
        "profilePhotoUrl",
        "facePhotoUrl",
    }:
        return validate_face_photo(
            normalized_slot,
            face_results,
        )

    return validate_pose_photo(
        normalized_slot,
        pose_results,
    )


def get_mobile_sam_predictor() -> SamPredictor:
    global mobile_sam_predictor

    if mobile_sam_predictor is None:
        with mobile_sam_lock:
            if mobile_sam_predictor is None:
                if not MOBILE_SAM_CHECKPOINT.is_file():
                    raise HTTPException(
                        status_code=503,
                        detail=(
                            "MobileSAM checkpoint was not found at "
                            f"{MOBILE_SAM_CHECKPOINT}."
                        ),
                    )

                model = sam_model_registry["vit_t"](
                    checkpoint=str(MOBILE_SAM_CHECKPOINT),
                )
                model.to(device=INFERENCE_DEVICE)
                model.eval()
                mobile_sam_predictor = SamPredictor(model)

    return mobile_sam_predictor


def parse_normalized_points(value: str, field_name: str) -> list[list[float]]:
    try:
        parsed = json.loads(value)
    except json.JSONDecodeError as error:
        raise HTTPException(
            status_code=400,
            detail=f"{field_name} must be a JSON array.",
        ) from error

    if not isinstance(parsed, list):
        raise HTTPException(
            status_code=400,
            detail=f"{field_name} must be a JSON array.",
        )

    points: list[list[float]] = []
    for point in parsed:
        if not isinstance(point, list) or len(point) != 2:
            raise HTTPException(
                status_code=400,
                detail=f"Every {field_name} entry must contain x and y.",
            )
        x, y = float(point[0]), float(point[1])
        if not (0.0 <= x <= 1.0 and 0.0 <= y <= 1.0):
            raise HTTPException(
                status_code=400,
                detail=f"{field_name} coordinates must be between 0 and 1.",
            )
        points.append([x, y])
    return points


@app.post("/segment-product")
def segment_product(
    positive_points: str = Form(...),
    negative_points: str = Form("[]"),
    image: UploadFile = File(...),
) -> Response:
    positives = parse_normalized_points(positive_points, "positive_points")
    negatives = parse_normalized_points(negative_points, "negative_points")

    if not positives:
        raise HTTPException(
            status_code=400,
            detail="Add at least one positive point on the product.",
        )

    source_image = read_pil_image(read_uploaded_image(image))
    image_array = np.asarray(source_image, dtype=np.uint8)
    height, width = image_array.shape[:2]
    all_points = positives + negatives
    point_coordinates = np.array(
        [[x * (width - 1), y * (height - 1)] for x, y in all_points],
        dtype=np.float32,
    )
    point_labels = np.array(
        [1] * len(positives) + [0] * len(negatives),
        dtype=np.int32,
    )

    predictor = get_mobile_sam_predictor()
    with mobile_sam_lock:
        predictor.set_image(image_array)
        masks, scores, _ = predictor.predict(
            point_coords=point_coordinates,
            point_labels=point_labels,
            multimask_output=True,
        )

    # Prefer a mask that obeys every admin point. SAM returns three candidate
    # masks, and the candidate with the highest model score can occasionally
    # include the background even when another candidate follows the prompts
    # more accurately.
    candidate_quality: list[float] = []
    integer_points = np.rint(point_coordinates).astype(np.int32)
    integer_points[:, 0] = np.clip(integer_points[:, 0], 0, width - 1)
    integer_points[:, 1] = np.clip(integer_points[:, 1], 0, height - 1)

    for candidate_mask, candidate_score in zip(masks, scores):
        sampled_values = candidate_mask[
            integer_points[:, 1],
            integer_points[:, 0],
        ]
        positive_hits = sampled_values[:len(positives)]
        negative_hits = sampled_values[len(positives):]
        prompt_accuracy = (
            float(np.mean(positive_hits))
            if len(positive_hits)
            else 0.0
        )
        if len(negative_hits):
            prompt_accuracy = (
                prompt_accuracy
                + float(np.mean(~negative_hits))
            ) / 2.0

        mask_ratio = float(np.mean(candidate_mask))
        area_penalty = 0.0
        if mask_ratio < 0.002 or mask_ratio > 0.96:
            area_penalty = 1.0

        candidate_quality.append(
            prompt_accuracy * 2.0
            + float(candidate_score)
            - area_penalty
        )

    best_index = int(np.argmax(candidate_quality))
    best_mask = masks[best_index].astype(np.uint8) * 255
    mask_pixels = int(np.count_nonzero(best_mask))
    image_pixels = width * height

    if mask_pixels < max(100, int(image_pixels * 0.001)):
        raise HTTPException(
            status_code=422,
            detail="The selected area is too small. Add another point on the product.",
        )

    output = BytesIO()
    Image.fromarray(best_mask, mode="L").save(output, format="PNG", optimize=True)
    return Response(
        content=output.getvalue(),
        media_type="image/png",
        headers={
            "X-Mask-Score": f"{float(scores[best_index]):.4f}",
            "X-Image-Width": str(width),
            "X-Image-Height": str(height),
            "X-Segmentation-Mode": "mobilesam-admin-approved",
        },
    )


@app.post("/recolor-product")
def recolor_product(
    target_hex: str = Form(...),
    item_type: str = Form("standalone-product"),
    image: UploadFile = File(...),
    mask: UploadFile = File(...),
) -> Response:
    normalized_hex = normalize_hex_color(target_hex)
    normalized_item_type = ITEM_TYPE_ALIASES.get(
        item_type.strip().lower(),
        item_type.strip().lower(),
    )
    if normalized_item_type == "auto":
        normalized_item_type = "standalone-product"

    source_image = read_pil_image(read_uploaded_image(image))
    mask_image = read_pil_image(read_uploaded_image(mask)).convert("L")
    mask_array = np.asarray(mask_image, dtype=np.uint8)
    if mask_array.shape != (source_image.height, source_image.width):
        mask_array = cv2.resize(
            mask_array,
            (source_image.width, source_image.height),
            interpolation=cv2.INTER_NEAREST,
        )
    approved_mask = prepare_approved_mask(mask_array >= 128)

    if not np.any(approved_mask):
        raise HTTPException(status_code=422, detail="The approved mask is empty.")

    recolored_image = apply_color(
        source_image,
        approved_mask,
        normalized_hex,
        normalized_item_type,
    )
    output = BytesIO()
    recolored_image.save(output, format="PNG", optimize=True)
    return Response(
        content=output.getvalue(),
        media_type="image/png",
        headers={
            "X-Detected-Item-Type": normalized_item_type,
            "X-Target-Color": normalized_hex,
            "X-Segmentation-Mode": "mobilesam-admin-approved",
            "X-Recolor-Engine": "approved-mask-local",
        },
    )


def read_uploaded_image(
    image: UploadFile,
) -> bytes:
    if image.content_type not in ALLOWED_CONTENT_TYPES:
        raise HTTPException(
            status_code=415,
            detail="Use a JPG, PNG, or WebP image.",
        )

    image_bytes = image.file.read(
        MAX_IMAGE_SIZE_BYTES + 1
    )

    if not image_bytes:
        raise HTTPException(
            status_code=400,
            detail="The uploaded image is empty.",
        )

    if len(image_bytes) > MAX_IMAGE_SIZE_BYTES:
        raise HTTPException(
            status_code=413,
            detail="Keep the image under 6 MB.",
        )

    return image_bytes


def read_pil_image(
    image_bytes: bytes,
) -> Image.Image:
    try:
        Image.MAX_IMAGE_PIXELS = MAX_IMAGE_PIXELS

        with Image.open(
            BytesIO(image_bytes)
        ) as source:
            image = ImageOps.exif_transpose(
                source
            ).convert("RGB")

            if (
                image.width * image.height
                > MAX_IMAGE_PIXELS
            ):
                raise HTTPException(
                    status_code=413,
                    detail=(
                        "Image dimensions are "
                        "too large."
                    ),
                )

            return image.copy()

    except HTTPException:
        raise

    except (
        UnidentifiedImageError,
        OSError,
        ValueError,
    ) as error:
        raise HTTPException(
            status_code=400,
            detail=(
                "This image could not be read."
            ),
        ) from error


def read_rgb_image(
    image_bytes: bytes,
) -> np.ndarray:
    image = read_pil_image(image_bytes)
    return np.asarray(image)


def get_clothing_model():
    global clothing_processor
    global clothing_model

    if (
        clothing_processor is not None
        and clothing_model is not None
    ):
        return clothing_processor, clothing_model

    with clothing_model_lock:
        if clothing_processor is None:
            clothing_processor = (
                AutoImageProcessor.from_pretrained(
                    CLOTHING_MODEL_NAME
                )
            )

        if clothing_model is None:
            clothing_model = (
                AutoModelForSemanticSegmentation
                .from_pretrained(
                    CLOTHING_MODEL_NAME
                )
            )

            clothing_model.eval()

    return clothing_processor, clothing_model


def contains_visible_face(
    image: Image.Image,
) -> bool:
    rgb_image = np.asarray(image)

    with detector_lock:
        results = face_detector.process(rgb_image)

    detections = (
        getattr(results, "detections", None)
        or []
    )

    return len(detections) > 0


def contains_visible_person(
    image: Image.Image,
) -> bool:
    rgb_image = np.asarray(image)

    with detector_lock:
        results = pose_detector.process(rgb_image)

    landmarks = getattr(
        results,
        "pose_landmarks",
        None,
    )

    if landmarks is None:
        return False

    visible_landmarks = sum(
        1
        for landmark in landmarks.landmark
        if is_landmark_visible(landmark)
    )

    return visible_landmarks >= 6


def segment_standalone_product(
    image: Image.Image,
) -> np.ndarray:
    rgb_image = np.asarray(image)
    height, width = rgb_image.shape[:2]

    if height < 40 or width < 40:
        raise HTTPException(
            status_code=422,
            detail="The product image is too small.",
        )

    lab_image = cv2.cvtColor(
        rgb_image,
        cv2.COLOR_RGB2LAB,
    ).astype(np.float32)

    border_size = max(
        5,
        min(height, width) // 35,
    )

    border_pixels = np.concatenate(
        [
            lab_image[:border_size].reshape(-1, 3),
            lab_image[-border_size:].reshape(-1, 3),
            lab_image[:, :border_size].reshape(-1, 3),
            lab_image[:, -border_size:].reshape(-1, 3),
        ],
        axis=0,
    )

    background_lab = np.median(
        border_pixels,
        axis=0,
    )

    color_distance = np.linalg.norm(
        lab_image - background_lab,
        axis=2,
    )

    border_distance = np.linalg.norm(
        border_pixels - background_lab,
        axis=1,
    )

    background_variation = float(
        np.percentile(border_distance, 90)
    )

    threshold = float(
        np.clip(
            background_variation + 12.0,
            16.0,
            42.0,
        )
    )

    probable_foreground = (
        color_distance > threshold
    ).astype(np.uint8)

    definite_foreground = (
        color_distance > threshold * 1.55
    ).astype(np.uint8)

    kernel_size = max(
        3,
        min(height, width) // 180,
    )

    if kernel_size % 2 == 0:
        kernel_size += 1

    kernel = cv2.getStructuringElement(
        cv2.MORPH_ELLIPSE,
        (kernel_size, kernel_size),
    )

    probable_foreground = cv2.morphologyEx(
        probable_foreground,
        cv2.MORPH_OPEN,
        kernel,
        iterations=1,
    )

    probable_foreground = cv2.morphologyEx(
        probable_foreground,
        cv2.MORPH_CLOSE,
        kernel,
        iterations=2,
    )

    grabcut_mask = np.full(
        (height, width),
        cv2.GC_PR_BGD,
        dtype=np.uint8,
    )

    grabcut_mask[color_distance < threshold * 0.65] = cv2.GC_BGD
    grabcut_mask[probable_foreground > 0] = cv2.GC_PR_FGD
    grabcut_mask[definite_foreground > 0] = cv2.GC_FGD

    grabcut_mask[:border_size, :] = cv2.GC_BGD
    grabcut_mask[-border_size:, :] = cv2.GC_BGD
    grabcut_mask[:, :border_size] = cv2.GC_BGD
    grabcut_mask[:, -border_size:] = cv2.GC_BGD

    background_model = np.zeros(
        (1, 65), dtype=np.float64
    )

    foreground_model = np.zeros(
        (1, 65), dtype=np.float64
    )

    try:
        cv2.grabCut(
            rgb_image,
            grabcut_mask,
            None,
            background_model,
            foreground_model,
            5,
            cv2.GC_INIT_WITH_MASK,
        )

        refined_mask = np.where(
            (grabcut_mask == cv2.GC_FGD)
            | (grabcut_mask == cv2.GC_PR_FGD),
            1,
            0,
        ).astype(np.uint8)
    except cv2.error:
        refined_mask = probable_foreground

    refined_mask = cv2.morphologyEx(
        refined_mask,
        cv2.MORPH_OPEN,
        kernel,
        iterations=1,
    )

    refined_mask = cv2.morphologyEx(
        refined_mask,
        cv2.MORPH_CLOSE,
        kernel,
        iterations=2,
    )

    component_count, labels, statistics, _ = (
        cv2.connectedComponentsWithStats(
            refined_mask,
            connectivity=8,
        )
    )

    if component_count <= 1:
        raise HTTPException(
            status_code=422,
            detail=(
                "The product could not be separated "
                "from its background. Use a plain, "
                "contrasting background."
            ),
        )

    component_areas = statistics[1:, cv2.CC_STAT_AREA]
    largest_area = int(component_areas.max())
    minimum_component_area = max(
        int(height * width * 0.001),
        int(largest_area * 0.04),
    )

    final_mask = np.zeros(
        (height, width),
        dtype=np.uint8,
    )

    for component_id in range(1, component_count):
        component_area = int(
            statistics[component_id, cv2.CC_STAT_AREA]
        )

        if component_area >= minimum_component_area:
            final_mask[labels == component_id] = 1

    mask_ratio = float(
        np.count_nonzero(final_mask)
    ) / float(height * width)

    if mask_ratio < 0.025:
        raise HTTPException(
            status_code=422,
            detail=(
                "The detected product is too "
                "small. Use a closer product "
                "photograph."
            ),
        )

    if mask_ratio > 0.88:
        raise HTTPException(
            status_code=422,
            detail=(
                "The product cannot be separated "
                "from the background. Use a plain, "
                "contrasting background."
            ),
        )

    return final_mask.astype(bool)


def segment_clothing(
    image: Image.Image,
) -> np.ndarray:
    processor, model = get_clothing_model()

    inputs = processor(
        images=image,
        return_tensors="pt",
    )

    with clothing_model_lock:
        with torch.inference_mode():
            output = model(**inputs)

    resized_logits = functional.interpolate(
        output.logits,
        size=(image.height, image.width),
        mode="bilinear",
        align_corners=False,
    )

    segmentation = (
        resized_logits
        .argmax(dim=1)[0]
        .cpu()
        .numpy()
        .astype(np.uint8)
    )

    return segmentation


def select_garment_mask(
    segmentation: np.ndarray,
    requested_item_type: str,
) -> tuple[str, np.ndarray]:
    if requested_item_type == "auto":
        detected_item_type = max(
            GARMENT_LABELS,
            key=lambda name: count_label_pixels(
                segmentation,
                GARMENT_LABELS[name],
            ),
        )
    else:
        detected_item_type = (
            requested_item_type
        )

    selected_labels = GARMENT_LABELS[
        detected_item_type
    ]

    garment_mask = np.isin(
        segmentation,
        list(selected_labels),
    )

    minimum_pixels = max(
        100,
        int(segmentation.size * 0.002),
    )

    detected_pixels = int(
        garment_mask.sum()
    )

    if detected_pixels < minimum_pixels:
        raise HTTPException(
            status_code=422,
            detail=(
                "The requested clothing item "
                "could not be detected clearly. "
                "Use a clear product photo with "
                "the complete item visible."
            ),
        )

    return detected_item_type, garment_mask


def count_label_pixels(
    segmentation: np.ndarray,
    labels: set[int],
) -> int:
    return int(
        np.isin(
            segmentation,
            list(labels),
        ).sum()
    )


def normalize_hex_color(
    value: str,
) -> str:
    normalized = value.strip().upper()

    if not normalized.startswith("#"):
        normalized = f"#{normalized}"

    if len(normalized) != 7:
        raise HTTPException(
            status_code=400,
            detail=(
                "Color must use the #RRGGBB "
                "format."
            ),
        )

    try:
        int(normalized[1:], 16)
    except ValueError as error:
        raise HTTPException(
            status_code=400,
            detail=(
                "Color must use the #RRGGBB "
                "format."
            ),
        ) from error

    return normalized


def hex_to_rgb(
    hex_color: str,
) -> np.ndarray:
    return np.array(
        [
            int(hex_color[1:3], 16),
            int(hex_color[3:5], 16),
            int(hex_color[5:7], 16),
        ],
        dtype=np.float32,
    )


def clean_semantic_mask(
    garment_mask: np.ndarray,
    item_type: str,
) -> np.ndarray:
    mask = garment_mask.astype(np.uint8)
    height, width = mask.shape

    kernel_size = max(
        3,
        min(height, width) // 250,
    )

    if kernel_size % 2 == 0:
        kernel_size += 1

    kernel = cv2.getStructuringElement(
        cv2.MORPH_ELLIPSE,
        (kernel_size, kernel_size),
    )

    mask = cv2.morphologyEx(
        mask,
        cv2.MORPH_CLOSE,
        kernel,
        iterations=1,
    )

    component_count, labels, statistics, _ = (
        cv2.connectedComponentsWithStats(
            mask,
            connectivity=8,
        )
    )

    if component_count <= 1:
        return garment_mask.astype(bool)

    components = [
        (
            component_id,
            int(
                statistics[
                    component_id,
                    cv2.CC_STAT_AREA,
                ]
            ),
        )
        for component_id in range(1, component_count)
    ]

    components.sort(
        key=lambda component: component[1],
        reverse=True,
    )

    largest_area = components[0][1]
    # A garment or bag should normally be one connected object. Shoes can
    # legitimately contain a left and right component.
    maximum_components = 2 if item_type == "shoes" else 1

    cleaned_mask = np.zeros_like(mask)

    for component_id, area in components[:maximum_components]:
        if area >= max(30, int(largest_area * 0.025)):
            cleaned_mask[labels == component_id] = 1

    minimum_pixels = max(
        100,
        int(height * width * 0.001),
    )

    if int(cleaned_mask.sum()) < minimum_pixels:
        return garment_mask.astype(bool)

    return cleaned_mask.astype(bool)


def prepare_approved_mask(
    approved_mask: np.ndarray,
) -> np.ndarray:
    """Remove isolated mask noise without discarding valid product pieces."""
    mask = approved_mask.astype(np.uint8)
    height, width = mask.shape

    kernel_size = max(3, min(height, width) // 350)
    if kernel_size % 2 == 0:
        kernel_size += 1

    kernel = cv2.getStructuringElement(
        cv2.MORPH_ELLIPSE,
        (kernel_size, kernel_size),
    )
    mask = cv2.morphologyEx(
        mask,
        cv2.MORPH_CLOSE,
        kernel,
        iterations=1,
    )

    component_count, labels, statistics, _ = (
        cv2.connectedComponentsWithStats(mask, connectivity=8)
    )
    if component_count <= 1:
        return approved_mask.astype(bool)

    component_areas = statistics[1:, cv2.CC_STAT_AREA]
    largest_area = int(component_areas.max())
    minimum_area = max(
        20,
        int(height * width * 0.00015),
        int(largest_area * 0.003),
    )

    cleaned = np.zeros_like(mask)
    for component_id in range(1, component_count):
        if int(statistics[component_id, cv2.CC_STAT_AREA]) >= minimum_area:
            cleaned[labels == component_id] = 1

    return cleaned.astype(bool)


def apply_color(
    image: Image.Image,
    garment_mask: np.ndarray,
    target_hex: str,
    item_type: str,
) -> Image.Image:
    original_rgb = np.asarray(
        image,
        dtype=np.uint8,
    )

    if garment_mask.shape != original_rgb.shape[:2]:
        garment_mask = cv2.resize(
            garment_mask.astype(np.uint8),
            (original_rgb.shape[1], original_rgb.shape[0]),
            interpolation=cv2.INTER_NEAREST,
        ).astype(bool)

    if not np.any(garment_mask):
        raise HTTPException(
            status_code=422,
            detail="No product area was detected.",
        )

    original_lab = cv2.cvtColor(
        original_rgb,
        cv2.COLOR_RGB2LAB,
    ).astype(np.float32)

    target_rgb = hex_to_rgb(target_hex).astype(np.uint8)
    target_pixel = target_rgb.reshape(1, 1, 3)

    target_lab = cv2.cvtColor(
        target_pixel,
        cv2.COLOR_RGB2LAB,
    )[0, 0].astype(np.float32)

    source_lightness = original_lab[:, :, 0]

    garment_lightness = source_lightness[
        garment_mask
    ]

    source_median = float(
        np.median(garment_lightness)
    )

    low_percentile = float(
        np.percentile(garment_lightness, 4)
    )

    high_percentile = float(
        np.percentile(garment_lightness, 96)
    )

    source_range = max(
        high_percentile - low_percentile,
        18.0,
    )

    normalized_detail = (
        source_lightness - source_median
    ) / source_range

    contrast = RECOLOR_CONTRAST.get(
        item_type,
        0.80,
    )

    output_lightness = (
        target_lab[0]
        + normalized_detail * 150.0 * contrast
    )

    output_lightness = np.clip(
        output_lightness,
        4.0,
        250.0,
    )

    # Preserve subtle local chroma variation so seams, weave, prints, and
    # material changes do not become a flat block of color. Most of the source
    # hue is replaced, while a small high-frequency residual is retained.
    source_a = original_lab[:, :, 1]
    source_b = original_lab[:, :, 2]
    garment_a_median = float(np.median(source_a[garment_mask]))
    garment_b_median = float(np.median(source_b[garment_mask]))

    local_a = source_a - cv2.GaussianBlur(
        source_a,
        (0, 0),
        sigmaX=4.0,
    )
    local_b = source_b - cv2.GaussianBlur(
        source_b,
        (0, 0),
        sigmaX=4.0,
    )

    # Very bright highlights and deep shadows naturally carry less visible
    # color. Scaling chroma there produces a more photographic result.
    normalized_lightness = np.clip(output_lightness / 255.0, 0.0, 1.0)
    chroma_visibility = np.clip(
        1.18 - np.abs(normalized_lightness - 0.52) * 0.72,
        0.62,
        1.0,
    )
    target_a_offset = target_lab[1] - 128.0
    target_b_offset = target_lab[2] - 128.0

    output_a = (
        128.0
        + target_a_offset * chroma_visibility
        + local_a * 0.20
        + (source_a - garment_a_median) * 0.035
    )
    output_b = (
        128.0
        + target_b_offset * chroma_visibility
        + local_b * 0.20
        + (source_b - garment_b_median) * 0.035
    )

    recolored_lab = original_lab.copy()
    recolored_lab[:, :, 0] = output_lightness
    recolored_lab[:, :, 1] = np.clip(output_a, 0.0, 255.0)
    recolored_lab[:, :, 2] = np.clip(output_b, 0.0, 255.0)

    recolored_rgb = cv2.cvtColor(
        np.clip(recolored_lab, 0, 255).astype(np.uint8),
        cv2.COLOR_LAB2RGB,
    ).astype(np.float32)

    original_float = original_rgb.astype(np.float32)

    original_gray = cv2.cvtColor(
        original_rgb,
        cv2.COLOR_RGB2GRAY,
    ).astype(np.float32)

    smooth_gray = cv2.GaussianBlur(
        original_gray,
        (0, 0),
        sigmaX=2.0,
    )

    fine_detail = (
        original_gray - smooth_gray
    )[:, :, None]

    detail_strength = (
        0.34
        if item_type in {"shoes", "bag"}
        else 0.27
    )

    recolored_rgb = np.clip(
        recolored_rgb + fine_detail * detail_strength,
        0,
        255,
    )

    mask_uint8 = garment_mask.astype(np.uint8) * 255

    edge_radius = max(
        0.8,
        min(image.width, image.height) / 900.0,
    )

    feathered_mask = cv2.GaussianBlur(
        mask_uint8,
        (0, 0),
        sigmaX=edge_radius,
        sigmaY=edge_radius,
    ).astype(np.float32) / 255.0

    alpha = feathered_mask[:, :, None]

    result = (
        original_float * (1.0 - alpha)
        + recolored_rgb * alpha
    )

    return Image.fromarray(
        np.clip(result, 0, 255).astype(np.uint8),
        mode="RGB",
    )


def validate_face_photo(
    slot: str,
    results: object,
) -> ValidationResponse:
    detections = (
        getattr(results, "detections", None)
        or []
    )

    face_count = len(detections)

    if face_count == 0:
        return rejected(
            slot,
            (
                "No clear human face was detected. "
                "Upload a face or profile photo, "
                "not a product or object."
            ),
            {
                "faceCount": 0,
            },
        )

    if face_count > 1:
        return rejected(
            slot,
            "Use a photo with one clear face only.",
            {
                "faceCount": face_count,
            },
        )

    bounding_box = (
        detections[0]
        .location_data
        .relative_bounding_box
    )

    face_width = max(
        0.0,
        bounding_box.width,
    )

    face_height = max(
        0.0,
        bounding_box.height,
    )

    if (
        face_width < 0.12
        or face_height < 0.12
    ):
        return rejected(
            slot,
            (
                "Move closer to the camera so "
                "your face is clearly visible."
            ),
            {
                "faceCount": 1,
                "faceWidth": round(
                    face_width,
                    3,
                ),
                "faceHeight": round(
                    face_height,
                    3,
                ),
            },
        )

    return accepted(
        slot,
        "A clear face was detected.",
        {
            "faceCount": 1,
            "faceWidth": round(
                face_width,
                3,
            ),
            "faceHeight": round(
                face_height,
                3,
            ),
        },
    )


def validate_pose_photo(
    slot: str,
    results: object | None,
) -> ValidationResponse:
    landmarks = getattr(
        results,
        "pose_landmarks",
        None,
    )

    if landmarks is None:
        return rejected(
            slot,
            (
                "No person was detected. Upload "
                "a clear clothed photo of yourself."
            ),
            {
                "visibleGroups": [],
            },
        )

    required_groups = POSE_REQUIREMENTS[slot]

    visible_groups = [
        group_name
        for (
            group_name,
            landmark_indices,
        ) in required_groups.items()
        if any(
            is_landmark_visible(
                landmarks.landmark[index]
            )
            for index in landmark_indices
        )
    ]

    missing_groups = [
        name
        for name in required_groups
        if name not in visible_groups
    ]

    if missing_groups:
        readable_missing = ", ".join(
            missing_groups
        )

        return rejected(
            slot,
            (
                "This photo is missing clearly "
                f"visible: {readable_missing}."
            ),
            {
                "visibleGroups": visible_groups,
                "missingGroups": missing_groups,
            },
        )

    return accepted(
        slot,
        (
            "The required body areas were "
            "detected."
        ),
        {
            "visibleGroups": visible_groups,
            "missingGroups": [],
        },
    )


def is_landmark_visible(
    landmark: object,
) -> bool:
    visibility = getattr(
        landmark,
        "visibility",
        0.0,
    )

    x = getattr(
        landmark,
        "x",
        -1.0,
    )

    y = getattr(
        landmark,
        "y",
        -1.0,
    )

    return (
        visibility >= 0.55
        and 0.0 <= x <= 1.0
        and 0.0 <= y <= 1.0
    )


def accepted(
    slot: str,
    message: str,
    checks: dict[str, object],
) -> ValidationResponse:
    return ValidationResponse(
        accepted=True,
        message=message,
        slot=slot,
        checks=checks,
    )


def rejected(
    slot: str,
    message: str,
    checks: dict[str, object],
) -> ValidationResponse:
    return ValidationResponse(
        accepted=False,
        message=message,
        slot=slot,
        checks=checks,
    )