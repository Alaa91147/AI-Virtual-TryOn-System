import { request } from './authService.js';

const AI_SERVICE_URL =
  import.meta.env.VITE_AI_SERVICE_URL ||
  'http://127.0.0.1:8000';

async function readAiError(response, fallback) {
  try {
    const error = await response.json();
    if (error?.detail) {
      return Array.isArray(error.detail)
        ? error.detail.map((item) => item.msg).join(', ')
        : error.detail;
    }
  } catch {
    // Keep the fallback message when the response is not JSON.
  }
  return fallback;
}

export const adminProductService = {
  getCatalog(token) {
    return request('/api/admin/products', {
      method: 'GET',
      token,
    });
  },

  create(token, product) {
    return request('/api/admin/products', {
      method: 'POST',
      token,
      body: JSON.stringify(product),
    });
  },

  update(token, productId, product) {
    return request(
      `/api/admin/products/${encodeURIComponent(productId)}`,
      {
        method: 'PUT',
        token,
        body: JSON.stringify(product),
      },
    );
  },

  uploadImage(token, file) {
    const body = new FormData();
    body.append('image', file);
    return request('/api/admin/products/image', {
      method: 'POST',
      token,
      body,
    });
  },

  async segmentImage(file, positivePoints, negativePoints = []) {
    const body = new FormData();
    body.append('image', file);
    body.append('positive_points', JSON.stringify(positivePoints));
    body.append('negative_points', JSON.stringify(negativePoints));

    const response = await fetch(`${AI_SERVICE_URL}/segment-product`, {
      method: 'POST',
      body,
    });

    if (!response.ok) {
      throw new Error(await readAiError(
        response,
        'MobileSAM could not isolate the selected product.',
      ));
    }

    return {
      blob: await response.blob(),
      score: Number(response.headers.get('X-Mask-Score') || 0),
    };
  },

  async recolorImage(file, maskFile, targetHex, itemType) {
    const body = new FormData();
    body.append('image', file);
    body.append('mask', maskFile);
    body.append('target_hex', targetHex);
    body.append('item_type', itemType);

    const response = await fetch(`${AI_SERVICE_URL}/recolor-product`, {
      method: 'POST',
      body,
    });

    if (!response.ok) {
      throw new Error(await readAiError(
        response,
        'The approved mask could not be recolored.',
      ));
    }

    return response.blob();
  },
};