import {
  Heart,
  Sparkles,
  UserRound,
  UsersRound,
} from 'lucide-react';

const options = [
  {
    value: 'women',
    title: 'Shop Women',
    description:
      "Show women's categories and recommendations.",
    icon: Heart,
  },
  {
    value: 'men',
    title: 'Shop Men',
    description:
      "Show men's categories and recommendations.",
    icon: UserRound,
  },
  {
    value: 'both',
    title: 'Shop Both',
    description:
      "Show men's and women's collections together.",
    icon: UsersRound,
  },
];

export default function ShoppingPreferencePrompt({
  onSelect,
  saving,
  error,
}) {
  return (
    <main className="preference-screen">
      <section className="preference-card">
        <div className="preference-heading">
          <span className="preference-icon">
            <Sparkles size={25} />
          </span>

          <span className="shop-eyebrow">
            Personalize your shop
          </span>

          <h1>What would you like to shop?</h1>

          <p>
            Choose what you want to see. You can
            change this later from your profile.
          </p>
        </div>

        {error ? (
          <div
            className="alert alert-error"
            role="alert"
          >
            {error}
          </div>
        ) : null}

        <div className="preference-options">
          {options.map((option) => {
            const Icon = option.icon;

            return (
              <button
                key={option.value}
                type="button"
                disabled={saving}
                onClick={() =>
                  onSelect(option.value)
                }
              >
                <span>
                  <Icon size={27} />
                </span>

                <strong>{option.title}</strong>

                <small>
                  {option.description}
                </small>
              </button>
            );
          })}
        </div>

        {saving ? (
          <p className="preference-saving">
            <span className="spinner small" />
            Saving your preference...
          </p>
        ) : null}
      </section>
    </main>
  );
}