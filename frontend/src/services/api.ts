const API_BASE_URL = "http://localhost:5157/api";

export async function getHome() {
  const response = await fetch(`${API_BASE_URL}/public/home`);

  if (!response.ok) {
    throw new Error("Failed to fetch home data");
  }

  return response.json();
}

export async function getFeatures() {
  const response = await fetch(`${API_BASE_URL}/public/features`);

  if (!response.ok) {
    throw new Error("Failed to fetch features");
  }

  return response.json();
}