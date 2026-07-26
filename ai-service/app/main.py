import os
from io import BytesIO
from threading import Lock
from typing import Final

import mediapipe as mp
import numpy as np
from fastapi import FastAPI, File, Form, HTTPException, UploadFile
from fastapi.middleware.cors import CORSMiddleware
from PIL import Image, ImageOps, UnidentifiedImageError
from pydantic import BaseModel

MAX_IMAGE_SIZE_BYTES: Final = 6 * 1024 * 1024
MAX_IMAGE_PIXELS: Final = 20_000_000
ALLOWED_CONTENT_TYPES: Final = {"image/jpeg", "image/png", "image/webp"}

# Each entry means that at least one landmark in the named group must be visible.
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


app = FastAPI(title="Virtual Try-On Image Validation", version="1.0.0")

allowed_origins = [
    origin.strip()
    for origin in os.getenv(
        "ALLOWED_ORIGINS",
        "http://localhost:5173,http://127.0.0.1:5173",
    ).split(",")
    if origin.strip()
]
app.add_middleware(
    CORSMiddleware,
    allow_origins=allowed_origins,
    allow_credentials=False,
    allow_methods=["POST"],
    allow_headers=["Content-Type"],
)

# MediaPipe solution models are included in the MediaPipe package. Keep one instance
# of each model and serialize access because their image-processing methods are stateful.
face_detector = mp.solutions.face_detection.FaceDetection(
    model_selection=0,
    min_detection_confidence=0.6,
)
pose_detector = mp.solutions.pose.Pose(
    static_image_mode=True,
    model_complexity=1,
    enable_segmentation=False,
    min_detection_confidence=0.6,
)
detector_lock = Lock()


@app.get("/health")
def health() -> dict[str, str]:
    return {"status": "ok", "engine": "MediaPipe"}


@app.post("/validate-image", response_model=ValidationResponse)
def validate_image(
    slot: str = Form(...),
    image: UploadFile = File(...),
) -> ValidationResponse:
    normalized_slot = slot.strip()
    if normalized_slot not in {"profilePhotoUrl", "facePhotoUrl", *POSE_REQUIREMENTS}:
        raise HTTPException(status_code=400, detail="Unsupported image slot.")

    if image.content_type not in ALLOWED_CONTENT_TYPES:
        raise HTTPException(status_code=415, detail="Use a JPG, PNG, or WebP image.")

    image_bytes = image.file.read(MAX_IMAGE_SIZE_BYTES + 1)
    if len(image_bytes) > MAX_IMAGE_SIZE_BYTES:
        raise HTTPException(status_code=413, detail="Keep the image under 6 MB.")

    rgb_image = read_rgb_image(image_bytes)

    with detector_lock:
        face_results = face_detector.process(rgb_image)
        pose_results = pose_detector.process(rgb_image) if normalized_slot in POSE_REQUIREMENTS else None

    if normalized_slot in {"profilePhotoUrl", "facePhotoUrl"}:
        return validate_face_photo(normalized_slot, face_results)

    return validate_pose_photo(normalized_slot, pose_results)


def read_rgb_image(image_bytes: bytes) -> np.ndarray:
    try:
        with Image.open(BytesIO(image_bytes)) as source:
            Image.MAX_IMAGE_PIXELS = MAX_IMAGE_PIXELS
            image = ImageOps.exif_transpose(source).convert("RGB")
            if image.width * image.height > MAX_IMAGE_PIXELS:
                raise HTTPException(status_code=413, detail="Image dimensions are too large.")
            return np.asarray(image)
    except HTTPException:
        raise
    except (UnidentifiedImageError, OSError, ValueError) as error:
        raise HTTPException(status_code=400, detail="This image could not be read.") from error


def validate_face_photo(slot: str, results: object) -> ValidationResponse:
    detections = getattr(results, "detections", None) or []
    face_count = len(detections)

    if face_count == 0:
        return rejected(slot, "No clear human face was detected. Upload a face or profile photo, not a product or object.", {
            "faceCount": 0,
        })

    if face_count > 1:
        return rejected(slot, "Use a photo with one clear face only.", {"faceCount": face_count})

    bounding_box = detections[0].location_data.relative_bounding_box
    face_width = max(0.0, bounding_box.width)
    face_height = max(0.0, bounding_box.height)
    if face_width < 0.12 or face_height < 0.12:
        return rejected(slot, "Move closer to the camera so your face is clearly visible.", {
            "faceCount": 1,
            "faceWidth": round(face_width, 3),
            "faceHeight": round(face_height, 3),
        })

    return accepted(slot, "A clear face was detected.", {
        "faceCount": 1,
        "faceWidth": round(face_width, 3),
        "faceHeight": round(face_height, 3),
    })


def validate_pose_photo(slot: str, results: object | None) -> ValidationResponse:
    landmarks = getattr(results, "pose_landmarks", None)
    if landmarks is None:
        return rejected(slot, "No person was detected. Upload a clear clothed photo of yourself.", {
            "visibleGroups": [],
        })

    required_groups = POSE_REQUIREMENTS[slot]
    visible_groups = [
        group_name
        for group_name, landmark_indices in required_groups.items()
        if any(is_landmark_visible(landmarks.landmark[index]) for index in landmark_indices)
    ]
    missing_groups = [name for name in required_groups if name not in visible_groups]

    if missing_groups:
        readable_missing = ", ".join(missing_groups)
        return rejected(slot, f"This photo is missing clearly visible: {readable_missing}.", {
            "visibleGroups": visible_groups,
            "missingGroups": missing_groups,
        })

    return accepted(slot, "The required body areas were detected.", {
        "visibleGroups": visible_groups,
        "missingGroups": [],
    })


def is_landmark_visible(landmark: object) -> bool:
    visibility = getattr(landmark, "visibility", 0.0)
    x = getattr(landmark, "x", -1.0)
    y = getattr(landmark, "y", -1.0)
    return visibility >= 0.55 and 0.0 <= x <= 1.0 and 0.0 <= y <= 1.0


def accepted(slot: str, message: str, checks: dict[str, object]) -> ValidationResponse:
    return ValidationResponse(accepted=True, message=message, slot=slot, checks=checks)


def rejected(slot: str, message: str, checks: dict[str, object]) -> ValidationResponse:
    return ValidationResponse(accepted=False, message=message, slot=slot, checks=checks)
