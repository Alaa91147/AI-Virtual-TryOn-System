# MediaPipe Image Validation Service

This local service checks profile and try-on uploads before the frontend saves them. It accepts normal clothed images; it does not require or evaluate nudity.

## Checks

| Upload type | MediaPipe validation |
| --- | --- |
| Profile / face | Exactly one clear, sufficiently large human face |
| Full body | Head, shoulders, hips, knees, and ankles |
| Upper body | Head, shoulders, and hips/torso |
| Lower body | Hips, knees, and ankles |

MediaPipe checks whether a suitable person is visible. It cannot guarantee garment fit, photo quality in every lighting condition, or identify the person.

## Run locally

Use Python 3.10 to 3.12 on Windows.

```powershell
cd ai-service
py -3.12 -m venv .venv
.\.venv\Scripts\Activate.ps1
pip install -r requirements.txt
uvicorn app.main:app --host 127.0.0.1 --port 8000 --reload
```

Verify the service at `http://127.0.0.1:8000/health`.

Then set this in `frontend/.env` and restart Vite:

```env
VITE_IMAGE_VALIDATION_URL=http://127.0.0.1:8000/validate-image
```

The service receives the image only for validation and does not write it to disk.
