export function getPasswordStrength(password) {
  const checks = [
    password.length >= 8,
    /[a-z]/.test(password) && /[A-Z]/.test(password),
    /\d/.test(password),
    /[^A-Za-z0-9]/.test(password),
  ];

  const score = checks.filter(Boolean).length;

  if (!password) {
    return { score: 0, label: 'Required' };
  }

  if (score <= 1) {
    return { score, label: 'Weak' };
  }

  if (score === 2) {
    return { score, label: 'Fair' };
  }

  if (score === 3) {
    return { score, label: 'Good' };
  }

  return { score, label: 'Strong' };
}

export default function PasswordStrength({ password }) {
  const strength = getPasswordStrength(password);

  return (
    <div className="password-strength" aria-live="polite">
      <div className="strength-bars" aria-hidden="true">
        {[1, 2, 3, 4].map((item) => (
          <span key={item} className={item <= strength.score ? 'active' : ''} />
        ))}
      </div>
      <span>{strength.label}</span>
    </div>
  );
}
