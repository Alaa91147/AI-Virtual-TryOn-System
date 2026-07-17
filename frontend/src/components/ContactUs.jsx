function ContactUs() {
  function handleSubmit(event) {
    event.preventDefault();
  }

  return (
    <section className="contact-section" id="contact">
      <div>
        <p className="section-label">Contact Us</p>
        <h2>Have A Question?</h2>

        <p>
          Reach out for support, feedback, or collaboration related to the AI
          Virtual Try-On platform.
        </p>
      </div>

      <form className="contact-form" onSubmit={handleSubmit}>
        <input type="text" placeholder="Your name" required />
        <input type="email" placeholder="Your email" required />
        <textarea
          placeholder="Your message"
          rows={5}
          required
        />

        <button type="submit">Send Message →</button>
      </form>
    </section>
  );
}

export default ContactUs;