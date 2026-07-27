const ACCEPTED_IMAGE_TYPES = new Set(['image/jpeg', 'image/png', 'image/webp']);
const ACCEPTED_IMAGE_LABEL = 'JPG, PNG, or WebP';
const MAX_IMAGE_SIZE_MB = 6;
const MAX_IMAGE_SIZE_BYTES = MAX_IMAGE_SIZE_MB * 1024 * 1024;
const IMAGE_VALIDATION_URL = import.meta.env.VITE_IMAGE_VALIDATION_URL || '';

const validationRules = {
  profilePhotoUrl: {
    label: 'profile photo',
    orientation: 'portraitOrSquare',
    requirement: 'Upload a clear clothed portrait photo of your face, not a product or object.',
  },
  fullBodyPhotoUrl: {
    label: 'full body photo',
    orientation: 'portrait',
    requirement: 'Upload a normal clothed full body photo where your head, torso, legs, and shoes are visible.',
  },
  upperBodyPhotoUrl: {
    label: 'upper body photo',
    orientation: 'flexible',
    requirement: 'Upload a normal clothed upper body photo from face or shoulders to waist.',
  },
  lowerBodyPhotoUrl: {
    label: 'lower body photo',
    orientation: 'flexible',
    requirement: 'Upload a normal clothed lower body photo from waist to shoes.',
  },
  facePhotoUrl: {
    label: 'face photo',
    orientation: 'portraitOrSquare',
    requirement: 'Upload a clear clothed face photo for accessories, not an object or product.',
  },
};

export function getImageValidationRequirement(fieldName) {
  return validationRules[fieldName]?.requirement || 'Upload a clear image.';
}

export async function validateImageUploadBasics(file) {
  if (!file) {
    return;
  }

  if (!ACCEPTED_IMAGE_TYPES.has(file.type)) {
    throw new Error(`Use ${ACCEPTED_IMAGE_LABEL}.`);
  }

  if (file.size > MAX_IMAGE_SIZE_BYTES) {
    throw new Error(`Keep the image under ${MAX_IMAGE_SIZE_MB} MB.`);
  }

  return readImageDimensions(file);
}

export async function validateProfileImageUpload(fieldName, file) {
  const rule = validationRules[fieldName] || validationRules.profilePhotoUrl;
  const dimensions = await validateImageUploadBasics(file);

  if (rule.orientation === 'portrait' && dimensions.height < dimensions.width * 1.12) {
    throw new Error(`Use a portrait ${rule.label}. Landscape photos do not work well for try-on.`);
  }

  if (rule.orientation === 'portraitOrSquare') {
    const ratio = dimensions.width / dimensions.height;
    if (ratio < 0.58 || ratio > 1.55) {
      throw new Error(`Use a centered portrait or square ${rule.label}.`);
    }
  }

  await validateWithAiEndpoint(fieldName, file, rule);
}

function readImageDimensions(file) {
  return new Promise((resolve, reject) => {
    const objectUrl = URL.createObjectURL(file);
    const image = new window.Image();

    image.onload = () => {
      URL.revokeObjectURL(objectUrl);
      resolve({
        width: image.naturalWidth,
        height: image.naturalHeight,
      });
    };

    image.onerror = () => {
      URL.revokeObjectURL(objectUrl);
      reject(new Error('This image could not be read. Try another photo.'));
    };

    image.src = objectUrl;
  });
}

async function validateWithAiEndpoint(fieldName, file, rule) {
  if (!IMAGE_VALIDATION_URL) {
    return;
  }

  const formData = new FormData();
  formData.append('image', file);
  formData.append('slot', fieldName);
  formData.append('requirement', rule.requirement);

  const response = await fetch(IMAGE_VALIDATION_URL, {
    method: 'POST',
    body: formData,
  });

  let payload = null;
  const contentType = response.headers.get('content-type') || '';
  if (contentType.includes('application/json')) {
    payload = await response.json();
  }

  if (!response.ok) {
    throw new Error(
      payload?.message || payload?.detail || 'Could not validate this image. Try another photo.',
    );
  }

  if (payload?.accepted === false) {
    throw new Error(payload?.message || rule.requirement);
  }
}
