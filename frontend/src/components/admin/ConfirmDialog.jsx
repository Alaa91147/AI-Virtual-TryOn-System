import { AlertTriangle, X } from 'lucide-react';

export default function ConfirmDialog({ open, title, message, confirmLabel = 'Confirm', tone = 'danger', busy = false, onConfirm, onCancel }) {
  if (!open) return null;
  return <div className="confirm-backdrop" role="presentation" onMouseDown={onCancel}>
    <section className="confirm-dialog" role="alertdialog" aria-modal="true" aria-labelledby="confirm-title" onMouseDown={(event)=>event.stopPropagation()}>
      <button className="confirm-close" type="button" onClick={onCancel} aria-label="Close"><X size={18}/></button>
      <span className={`confirm-icon ${tone}`}><AlertTriangle size={22}/></span>
      <h2 id="confirm-title">{title}</h2><p>{message}</p>
      <footer><button type="button" className="admin-secondary-btn" onClick={onCancel} disabled={busy}>Cancel</button><button type="button" className={`confirm-action ${tone}`} onClick={onConfirm} disabled={busy}>{busy?'Working…':confirmLabel}</button></footer>
    </section>
  </div>;
}
