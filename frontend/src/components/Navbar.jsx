import { Link } from "react-router-dom";

function Navbar() {
  return (
    <header className="navbar">
      <a className="logo" href="#home">
        <span className="logo-icon">⌁</span>

        <span>
          <small>AI VIRTUAL</small>
          TRY-ON
        </span>
      </a>

      <nav className="nav-links">
        <a className="active" href="#home">
          Home
        </a>

        <a href="#features">Features</a>
        <a href="#how">How It Works</a>
        <a href="#about">About Us</a>
        <a href="#contact">Contact</a>
      </nav>

      <div className="nav-actions">
        <button
          className="theme-button"
          type="button"
          aria-label="Change theme"
        >
          ☼
        </button>

        <Link className="outline-btn" to="/auth/login">
          Log In
        </Link>

        <Link className="dark-btn" to="/auth/signup">
          Sign Up
        </Link>
      </div>
    </header>
  );
}

export default Navbar;