const steps = [
  [
    "01",
    "Upload Your Photo",
    "Start with a clear photo so the AI can understand your body shape and style.",
  ],
  [
    "02",
    "Choose Clothing",
    "Pick clothing items from your wardrobe or available outfit suggestions.",
  ],
  [
    "03",
    "Generate Try-On",
    "Preview a realistic AI try-on result in seconds.",
  ],
  [
    "04",
    "Save Your Look",
    "Save your favorite outfits and revisit them anytime.",
  ],
];

function HowItWorks() {
  return (
    <section className="how-section" id="how">
      <p className="section-label">How It Works</p>
      <h2>Try On Outfits In Four Simple Steps</h2>

      <div className="steps-grid">
        {steps.map(([number, title, text]) => (
          <article className="step-card" key={number}>
            <span>{number}</span>
            <h3>{title}</h3>
            <p>{text}</p>
          </article>
        ))}
      </div>
    </section>
  );
}

export default HowItWorks;