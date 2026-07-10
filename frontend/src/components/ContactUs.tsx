function ContactUs() {
  return (
    <section className="contact-section" id="contact">
      <div>
        <p className="section-label">Contact Us</p>
        <h2>Have A Question?</h2>
        <p>
          Reach out for support, feedback, or collaboration related to the AI Virtual
          Try-On platform.
        </p>
      </div>

      <form className="contact-form">
        <input type="text" placeholder="Your name" />
        <input type="email" placeholder="Your email" />
        <textarea placeholder="Your message" rows={5}></textarea>
        <button type="submit">Send Message →</button>
      </form>
    </section>
  );
}

export default ContactUs;