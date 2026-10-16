const API_BASE_URL = (
  import.meta.env.VITE_API_BASE_URL ||
  'http://localhost:5113'
).replace(/\/+$/, '');

const IMAGE_TYPES = ['image/jpeg', 'image/png', 'image/webp'];

async function loadImage(source, label) {
  let image = source;

  if (typeof source === 'string') {
    const response = await fetch(new URL(source, `${API_BASE_URL}/`), { cache: 'no-store' });

    if (!response.ok) {
      throw new Error(`Could not load ${label}.`);
    }

    image = await response.blob();
  }

  if (!(image instanceof Blob) || !IMAGE_TYPES.includes(image.type)) {
    throw new Error(`${label} must be JPG, PNG, or WebP.`);
  }

  if (!image.size || image.size > 6 * 1024 * 1024) {
    throw new Error(`${label} must be under 6 MB.`);
  }

  return image;
}

function filename(label, image) {
  const extension = {
    'image/jpeg': 'jpg',
    'image/png': 'png',
    'image/webp': 'webp',
  }[image.type];

  return `${label}.${extension}`;
}

export async function generateTryOn(token, personSource, garmentUrl) {
  const [person, garment] = await Promise.all([
    loadImage(personSource, 'Your photo'),
    loadImage(garmentUrl, 'The product image'),
  ]);

  const form = new FormData();
  form.append('person', person, filename('person', person));
  form.append('garment', garment, filename('garment', garment));

  const response = await fetch(`${API_BASE_URL}/api/try-on`, {
    method: 'POST',
    headers: { Authorization: `Bearer ${token}` },
    body: form,
  });

  if (!response.ok) {
    const data = await response.json().catch(() => null);
    throw new Error(data?.message || 'Could not generate a try-on image.');
  }

  const result = await response.blob();

  if (result.type !== 'image/png' || !result.size) {
    throw new Error('The AI service returned an invalid result.');
  }

  return result;
}