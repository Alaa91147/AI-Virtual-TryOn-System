const features = [
  [
    "▣",
    "Realistic AI Try-On",
    "Advanced AI ensures realistic results so you can shop with complete confidence.",
  ],
  [
    "▦",
    "Personal Wardrobe",
    "Save your favorite items, create outfits, and manage your wardrobe easily.",
  ],
  [
    "⌁",
    "Size Recommendation",
    "Get accurate size suggestions based on your measurements and body shape.",
  ],
  [
    "✧",
    "Outfit Inspiration",
    "Discover AI-curated outfit ideas tailored to your style and every occasion.",
  ],
];

function Features() {
  return (
    <section className="features" id="features">
      <p className="section-label">Why Choose Us</p>
      <h2>The Future Of Online Shopping</h2>

      <div className="feature-grid">
        {features.map(([icon, title, text]) => (
          <article className="feature-card" key={title}>
            <span>{icon}</span>
            <h3>{title}</h3>
            <p>{text}</p>
          </article>
        ))}
      </div>
    </section>
  );
}

export default Features;