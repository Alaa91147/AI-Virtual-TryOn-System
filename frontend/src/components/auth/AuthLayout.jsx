import { Sparkles } from 'lucide-react';

export default function AuthLayout({ title, subtitle, children }) {
  return (
    <main className="auth-shell">
      <section className="auth-visual auth-visual--left" aria-label="Women's virtual try-on preview">
        <img src="/assets/women_tryon.png" alt="Women's virtual try-on preview" />
      </section>

      <section className="auth-panel">
        <div className="auth-card">
          <div className="auth-brand">
            <Sparkles size={18} aria-hidden="true" />
            <span>AI Virtual Try-On</span>
          </div>
          <div className="auth-heading">
            <span className="auth-kicker">AI Virtual Try-On Shop</span>
            <h1>{title}</h1>
            {subtitle ? <p>{subtitle}</p> : null}
          </div>
          {children}
        </div>
      </section>

      <section className="auth-visual auth-visual--right" aria-label="Men's virtual try-on preview">
        <img src="/assets/men_tryon.png" alt="Men's virtual try-on preview" />
      </section>
    </main>
  );
}
