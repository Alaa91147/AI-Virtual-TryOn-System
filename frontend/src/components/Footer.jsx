function Footer() {
  function handleSubscribe(event) {
    event.preventDefault();
  }

  return (
    <footer className="footer">
      <div>
        <strong>Be the first to know</strong>
        <p>About new features and exclusive offers.</p>
      </div>

      <form className="subscribe-form" onSubmit={handleSubscribe}>
        <input
          type="email"
          placeholder="Enter your email"
          aria-label="Email address"
          required
        />

        <button type="submit">Subscribe →</button>
      </form>

      <div className="footer-thumbs">
        <span>✦</span>

        <img
          src="https://images.unsplash.com/photo-1503342217505-b0a15ec3261c?auto=format&fit=crop&w=140&q=80"
          alt="Fashion outfit"
        />

        <img
          src="https://images.unsplash.com/photo-1487412720507-e7ab37603c6f?auto=format&fit=crop&w=140&q=80"
          alt="Fashion portrait"
        />

        <img
          src="https://images.unsplash.com/photo-1496747611176-843222e1e57c?auto=format&fit=crop&w=140&q=80"
          alt="Fashion model"
        />
      </div>
    </footer>
  );
}

export default Footer;