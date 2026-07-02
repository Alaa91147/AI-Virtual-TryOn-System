import { Sparkles } from 'lucide-react';

export default function AuthLayout({ title, subtitle, children }) {
  return (
    <main className="auth-shell">
      <section className="auth-visual" aria-label="Virtual try-on fashion preview">
        <img src="/assets/virtual-tryon-auth-banner.png" alt="" />
        <div className="auth-visual__overlay">
          <div className="brand-mark">
            <Sparkles size={18} aria-hidden="true" />
            <span>AI Virtual Try-On</span>
          </div>
          <div className="fashion-chip">Smart fit</div>
        </div>
      </section>

      <section className="auth-panel">
        <div className="auth-card">
          <div className="auth-heading">
            <span className="auth-kicker">AI Virtual Try-On Shop</span>
            <h1>{title}</h1>
            {subtitle ? <p>{subtitle}</p> : null}
          </div>
          {children}
        </div>
      </section>
    </main>
  );
}
