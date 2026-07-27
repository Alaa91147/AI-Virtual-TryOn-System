import { useEffect, useRef, useState } from 'react';
import { Check, FlipHorizontal, LoaderCircle, RotateCcw, RotateCw, X } from 'lucide-react';

const FRAME_CONFIG = {
  fullBodyPhotoUrl: { width: 900, height: 1200, ratio: '3 / 4', guide: 'Keep your head, hands, legs, and shoes inside the frame.' },
  upperBodyPhotoUrl: { width: 1000, height: 1250, ratio: '4 / 5', guide: 'Keep your face, shoulders, torso, and waist visible.' },
  lowerBodyPhotoUrl: { width: 900, height: 1200, ratio: '3 / 4', guide: 'Keep your waist, knees, and shoes inside the frame.' },
  facePhotoUrl: { width: 900, height: 900, ratio: '1 / 1', guide: 'Center one clear face inside the oval guide.' },
  profilePhotoUrl: { width: 900, height: 900, ratio: '1 / 1', guide: 'Center one clear face inside the oval guide.' },
};

export default function ImageAdjuster({ file, fieldName, label, onApply, onCancel }) {
  const imageRef = useRef(null);
  const previewRef = useRef(null);
  const dragRef = useRef(null);
  const [sourceUrl, setSourceUrl] = useState('');
  const [zoom, setZoom] = useState(1);
  const [offsetX, setOffsetX] = useState(0);
  const [offsetY, setOffsetY] = useState(0);
  const [rotation, setRotation] = useState(0);
  const [flipped, setFlipped] = useState(false);
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState('');
  const frame = FRAME_CONFIG[fieldName] || FRAME_CONFIG.profilePhotoUrl;

  useEffect(() => {
    const objectUrl = URL.createObjectURL(file);
    setSourceUrl(objectUrl);

    return () => URL.revokeObjectURL(objectUrl);
  }, [file]);

  useEffect(() => {
    function handleKeyDown(event) {
      if (event.key === 'Escape' && !submitting) onCancel();
    }
    window.addEventListener('keydown', handleKeyDown);
    return () => window.removeEventListener('keydown', handleKeyDown);
  }, [onCancel, submitting]);

  function resetAdjustment() {
    setZoom(1);
    setOffsetX(0);
    setOffsetY(0);
    setRotation(0);
    setFlipped(false);
    setError('');
  }

  function moveImage(event) {
    const drag = dragRef.current;
    const preview = previewRef.current;
    if (!drag || !preview) return;
    const bounds = preview.getBoundingClientRect();
    setOffsetX(Math.max(-50, Math.min(50, drag.x + ((event.clientX - drag.clientX) / bounds.width) * 100)));
    setOffsetY(Math.max(-50, Math.min(50, drag.y + ((event.clientY - drag.clientY) / bounds.height) * 100)));
  }

  async function applyAdjustment() {
    const image = imageRef.current;
    if (!image || submitting) return;

    const canvas = document.createElement('canvas');
    canvas.width = frame.width;
    canvas.height = frame.height;
    const context = canvas.getContext('2d');
    if (!context) return;

    context.fillStyle = '#f4efe6';
    context.fillRect(0, 0, frame.width, frame.height);

    const fitScale = Math.min(frame.width / image.naturalWidth, frame.height / image.naturalHeight);
    context.save();
    context.translate(
      frame.width / 2 + (offsetX / 100) * frame.width,
      frame.height / 2 + (offsetY / 100) * frame.height,
    );
    context.rotate((rotation * Math.PI) / 180);
    context.scale(flipped ? -fitScale * zoom : fitScale * zoom, fitScale * zoom);
    context.drawImage(image, -image.naturalWidth / 2, -image.naturalHeight / 2);
    context.restore();

    setSubmitting(true);
    setError('');
    try {
      const blob = await new Promise((resolve) => canvas.toBlob(resolve, 'image/jpeg', 0.94));
      if (!blob) throw new Error('Could not create the adjusted image.');
      await onApply(new File([blob], `${file.name.replace(/\.[^.]+$/, '')}-adjusted.jpg`, {
        type: 'image/jpeg',
      }));
    } catch (applyError) {
      setError(applyError instanceof Error ? applyError.message : 'This adjusted image was not accepted.');
    } finally {
      setSubmitting(false);
    }
  }

  return (
    <div className="image-adjuster-backdrop" role="presentation" onMouseDown={(event) => {
      if (event.target === event.currentTarget && !submitting) onCancel();
    }}>
      <section className="image-adjuster" role="dialog" aria-modal="true" aria-labelledby="image-adjuster-title">
        <div className="image-adjuster-header">
          <div>
            <h2 id="image-adjuster-title">Adjust {label}</h2>
            <p>{frame.guide}</p>
          </div>
          <button type="button" className="icon-button" onClick={onCancel} disabled={submitting} aria-label="Close image editor">
            <X size={20} />
          </button>
        </div>

        <div
          ref={previewRef}
          className={`image-adjuster-preview image-adjuster-preview--${fieldName}`}
          style={{ aspectRatio: frame.ratio }}
          onPointerDown={(event) => {
            event.currentTarget.setPointerCapture(event.pointerId);
            dragRef.current = { clientX: event.clientX, clientY: event.clientY, x: offsetX, y: offsetY };
          }}
          onPointerMove={moveImage}
          onPointerUp={() => { dragRef.current = null; }}
          onPointerCancel={() => { dragRef.current = null; }}
        >
          {sourceUrl ? (
            <img
              ref={imageRef}
              src={sourceUrl}
              alt="Image being adjusted"
              draggable="false"
              style={{
                transform: `translate(calc(-50% + ${offsetX}%), calc(-50% + ${offsetY}%)) rotate(${rotation}deg) scale(${flipped ? -zoom : zoom}, ${zoom})`,
              }}
            />
          ) : null}
          <div className="image-adjuster-grid" aria-hidden="true" />
          {(fieldName === 'facePhotoUrl' || fieldName === 'profilePhotoUrl') ? (
            <div className="image-adjuster-face-guide" aria-hidden="true" />
          ) : null}
          <span className="image-adjuster-drag-hint">Drag to reposition</span>
        </div>

        <div className="image-adjuster-toolbar" aria-label="Image tools">
          <button type="button" onClick={() => setRotation((value) => value - 90)} title="Rotate left"><RotateCcw size={18} /> Rotate left</button>
          <button type="button" onClick={() => setRotation((value) => value + 90)} title="Rotate right"><RotateCw size={18} /> Rotate right</button>
          <button type="button" onClick={() => setFlipped((value) => !value)} className={flipped ? 'is-active' : ''}><FlipHorizontal size={18} /> Flip</button>
          <button type="button" onClick={resetAdjustment}>Reset</button>
        </div>

        <div className="image-adjuster-controls">
          <label><span>Zoom <strong>{Math.round(zoom * 100)}%</strong></span><input type="range" min="1" max="3" step="0.01" value={zoom} onChange={(event) => setZoom(Number(event.target.value))} /></label>
          <label><span>Horizontal</span><input type="range" min="-50" max="50" value={offsetX} onChange={(event) => setOffsetX(Number(event.target.value))} /></label>
          <label><span>Vertical</span><input type="range" min="-50" max="50" value={offsetY} onChange={(event) => setOffsetY(Number(event.target.value))} /></label>
        </div>

        <div className="image-adjuster-validation">
          <span className="image-adjuster-validation-icon"><Check size={15} /></span>
          <span><strong>Checked after adjustment</strong> — the final crop must still match the {label} requirements.</span>
        </div>
        {error ? <p className="field-error image-adjuster-error" role="alert">{error}</p> : null}

        <div className="image-adjuster-actions">
          <button type="button" className="secondary-button" onClick={onCancel} disabled={submitting}>Cancel</button>
          <button type="button" className="primary-button" onClick={applyAdjustment} disabled={submitting}>
            {submitting ? <LoaderCircle className="spin" size={18} /> : <Check size={18} />}
            {submitting ? 'Checking image…' : 'Apply & recheck'}
          </button>
        </div>
      </section>
    </div>
  );
}
