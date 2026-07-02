import { CalendarDays, LogOut, Mail, Ruler, ShieldCheck, UserRound } from 'lucide-react';
import { useAuth } from '../context/AuthContext.jsx';

function formatDate(value) {
  if (!value) {
    return 'Not added';
  }

  return new Intl.DateTimeFormat(undefined, {
    year: 'numeric',
    month: 'short',
    day: 'numeric',
  }).format(new Date(value));
}

function formatGender(value) {
  if (!value) {
    return 'Not added';
  }

  return value.charAt(0).toUpperCase() + value.slice(1);
}

export default function ProfilePage() {
  const { user, logout } = useAuth();

  return (
    <main className="profile-shell">
      <header className="profile-topbar">
        <div>
          <span className="auth-kicker">AI Virtual Try-On Shop</span>
          <h1>Profile</h1>
        </div>
        <button className="ghost-button" type="button" onClick={logout}>
          <LogOut size={18} aria-hidden="true" />
          <span>Logout</span>
        </button>
      </header>

      <section className="profile-grid">
        <article className="profile-summary">
          <div className="avatar" aria-hidden="true">
            {user?.fullName?.charAt(0).toUpperCase() || 'U'}
          </div>
          <div>
            <h2>{user?.fullName}</h2>
            <p>{user?.email}</p>
          </div>
        </article>

        <article className="profile-card">
          <h2>Account details</h2>
          <dl className="detail-list">
            <div>
              <dt>
                <UserRound size={17} aria-hidden="true" />
                Gender
              </dt>
              <dd>{formatGender(user?.gender)}</dd>
            </div>
            <div>
              <dt>
                <Mail size={17} aria-hidden="true" />
                Email
              </dt>
              <dd>{user?.email}</dd>
            </div>
            <div>
              <dt>
                <ShieldCheck size={17} aria-hidden="true" />
                Role
              </dt>
              <dd>{user?.role}</dd>
            </div>
            <div>
              <dt>
                <CalendarDays size={17} aria-hidden="true" />
                Date of birth
              </dt>
              <dd>{formatDate(user?.dateOfBirth)}</dd>
            </div>
          </dl>
        </article>

        <article className="profile-card fit-card">
          <h2>Fit profile</h2>
          <div className="fit-placeholder">
            <Ruler size={22} aria-hidden="true" />
            <div>
              <strong>Height, weight, and preferred size</strong>
              <span>Ready for the profile setup phase.</span>
            </div>
          </div>
        </article>
      </section>
    </main>
  );
}
