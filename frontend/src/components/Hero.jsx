import { Link } from "react-router-dom";
import heroBackground from "../assets/hero-bg.png";

function Hero() {
  return (
    <section className="hero" id="home">
      <div className="hero-content">
        <span className="badge">✦ AI Powered</span>

        <h1>
          Try On Confidence.
          <br />
          Discover <span>Your Style.</span>
        </h1>

        <p>
          Our AI technology lets you see how clothes look on you before you buy
          — anytime, anywhere.
        </p>

        <div className="hero-buttons">
          <Link className="dark-btn big" to="/auth/signup">
            Get Started Free <span>→</span>
          </Link>

          <a className="outline-btn big" href="#how">
            See How It Works <span>▷</span>
          </a>
        </div>

        <div className="trust-row">
          <div>
            <strong>♢ Secure & Private</strong>
            <small>Your data is safe</small>
          </div>

          <div>
            <strong>⚡ Realistic Results</strong>
            <small>AI powered try-on</small>
          </div>

          <div>
            <strong>▣ No Credit Card</strong>
            <small>Free to get started</small>
          </div>
        </div>
      </div>

      <div className="hero-image-panel">
        <img
          src={heroBackground}
          alt="AI virtual try-on fashion preview"
        />
      </div>
    </section>
  );
}

export default Hero;