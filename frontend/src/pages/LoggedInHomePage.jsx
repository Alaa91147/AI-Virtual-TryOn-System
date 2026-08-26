import {
  useEffect,
  useMemo,
  useState,
} from 'react';

import { Link } from 'react-router-dom';

import {
  Bell,
  ChevronDown,
  Heart,
  Search,
  ShoppingBag,
  Sparkles,
  Star,
  UserRound,
} from 'lucide-react';

import {
  favoriteService,
} from '../services/favoriteService.js';
import heroBackground from '../assets/shop-hero.png';

import femaleNewIn from '../assets/categories/category-new-in.png';
import femaleTops from '../assets/categories/category-tops.png';
import femaleDresses from '../assets/categories/category-dresses.png';
import femaleBottoms from '../assets/categories/category-bottoms.png';
import femaleOuterwear from '../assets/categories/category-outerwear.png';
import categoryShoes from '../assets/categories/category-shoes.png';
import categoryAccessories from '../assets/categories/category-accessories.png';

import maleNewIn from '../assets/categories/men/category-men-new-in.png';
import maleShirts from '../assets/categories/men/category-men-shirts.png';
import malePolos from '../assets/categories/men/category-men-polos.png';
import maleTrousers from '../assets/categories/men/category-men-trousers.png';
import maleOuterwear from '../assets/categories/men/category-men-outerwear.png';
import maleShoes from '../assets/categories/men/category-men-shoes.png';
import maleAccessories from '../assets/categories/men/category-men-accessories.png';

import ShoppingPreferencePrompt from '../components/shop/ShoppingPreferencePrompt.jsx';
import NotificationDropdown from '../components/shop/NotificationDropdown.jsx';


import { useAuth } from '../context/AuthContext.jsx';
import {
  getErrorMessage,
} from '../services/authService.js';
import { shopService } from '../services/shopService.js';
import { productService } from '../services/productService.js';
import { cartService } from '../services/cartService.js';

const categoryImages = {
  women: {
    'new-in': femaleNewIn,
    tops: femaleTops,
    dresses: femaleDresses,
    bottoms: femaleBottoms,
    outerwear: femaleOuterwear,
    shoes: categoryShoes,
    accessories: categoryAccessories,
  },

  men: {
    'new-in': maleNewIn,
    shirts: maleShirts,
    polos: malePolos,
    trousers: maleTrousers,
    outerwear: maleOuterwear,
    shoes: maleShoes,
    accessories: maleAccessories,
  },
};

function getCategoryImage(
  audience,
  slug,
) {
  return (
    categoryImages[audience]?.[slug] ||
    categoryImages.women['new-in']
  );
}

function ProductCard({
  product,
  favorite,
  onFavorite,
}) {
  const availableSize =
    product.sizes?.find(
      (size) => size.stockQuantity > 0,
    )?.name ||
    product.sizes?.[0]?.name ||
    'One Size';

  return (
    <article className="shop-product-card">
      <div className="shop-product-media">
        <Link
  className="shop-product-image-link"
  to={`/shop/products/${product.id}`}
  aria-label={`View ${product.name} details`}
>
  <img
    src={product.imageUrl}
    alt={product.name}
    loading="lazy"
  />
</Link>

        {product.badge ? (
          <span className="shop-product-badge">
            {product.badge}
          </span>
        ) : null}

        <button
          className={`shop-heart-button ${
            favorite ? 'is-favorite' : ''
          }`}
          type="button"
          aria-label={
            favorite
              ? `Remove ${product.name} from saved items`
              : `Add ${product.name} to saved items`
          }
          onClick={() =>
            onFavorite(product.id)
          }
        >
          <Heart
            size={18}
            fill={
              favorite
                ? 'currentColor'
                : 'none'
            }
          />
        </button>
      </div>

      <div className="shop-product-info">
        <div className="shop-product-heading">
          <div>
            <p className="shop-product-category">
              {product.categoryName}
            </p>

<h3>
  <Link
    className="shop-product-name-link"
    to={`/shop/products/${product.id}`}
  >
    {product.name}
  </Link>
</h3>          </div>

          <strong className={product.salePrice != null ? 'sale-price' : ''}>
            ${Number(product.salePrice ?? product.price).toFixed(2)}
          </strong>
          {product.salePrice != null ? (
            <small className="original-price">${Number(product.price).toFixed(2)}</small>
          ) : null}
        </div>

        <div
          className="shop-rating"
          aria-label={`${product.rating} out of 5 stars`}
        >
          <Star
            size={14}
            fill="currentColor"
          />

          <span>{product.rating}</span>

          <small>
            ({product.reviewCount})
          </small>
        </div>

        <div className="shop-product-options">
          <div
            className="shop-swatches"
            aria-label="Available colors"
          >
            {product.colors?.map((color) => (
              <span
                key={color.id}
                title={color.name}
                style={{
                  background:
                    color.hexCode,
                }}
              />
            ))}
          </div>

          <span className="shop-size">
            Your size:{' '}
            <strong>{availableSize}</strong>
          </span>
        </div>

        <button
          className="shop-try-button"
          type="button"
          disabled
          aria-disabled="true"
          title="AI Virtual Try-On is coming next"
        >
          <Sparkles size={15} />
          Try On
        </button>
      </div>
    </article>
  );
}

export default function LoggedInHomePage() {
  useEffect(() => {
  window.scrollTo(0, 0);
}, []);
  const {
    user,
    token,
    updateShoppingPreference,
  } = useAuth();

  const [shopHome, setShopHome] =
    useState(null);

  const [loading, setLoading] =
    useState(true);

  const [loadError, setLoadError] =
    useState('');

  const [
    preferenceError,
    setPreferenceError,
  ] = useState('');

  const [
    savingPreference,
    setSavingPreference,
  ] = useState(false);

  const [reloadKey, setReloadKey] =
    useState(0);

  const [activeFilter, setActiveFilter] =
    useState('all');

  const [query, setQuery] =
    useState('');

  const [favorites, setFavorites] =
    useState(new Set());

    const [cartCount, setCartCount] = useState(0);

    const [recentlyViewed, setRecentlyViewed] =
  useState([]);

const [showAllRecently, setShowAllRecently] =
  useState(false);

  const [favoriteError, setFavoriteError] =
    useState('');

  const [sort, setSort] =
    useState('recommended');
useEffect(() => {
  let cancelled = false;

  async function loadCartCount() {
    if (!token) {
      return;
    }

    try {
      const cart = await cartService.get(token);

      if (!cancelled) {
        setCartCount(cart.totalQuantity);
      }
    } catch {
      if (!cancelled) {
        setCartCount(0);
      }
    }
  }

  function handleCartUpdated(event) {
    setCartCount(Number(event.detail) || 0);
  }

  loadCartCount();

  window.addEventListener(
    'cart-updated',
    handleCartUpdated,
  );

  return () => {
    cancelled = true;

    window.removeEventListener(
      'cart-updated',
      handleCartUpdated,
    );
  };
}, [token, user?.id]);

  useEffect(() => {
    setShopHome(null);
    setFavorites(new Set());
    setActiveFilter('all');
    setQuery('');
    setLoadError('');
    setFavoriteError('');
    setLoading(true);
  }, [user?.id, token]);

  useEffect(() => {
    let cancelled = false;

    async function loadShopHome() {
      if (!token) {
        return;
      }

      setLoading(true);
      setLoadError('');

      try {
        const data =
          await shopService.getHome(token);

        if (!cancelled) {
          setShopHome(data);

          setFavorites(
            new Set(
              data.products
                .filter(
                  (product) =>
                    product.isFavorite,
                )
                .map(
                  (product) =>
                    product.id,
                ),
            ),
          );
        }
      } catch (error) {
        if (!cancelled) {
          setLoadError(
            getErrorMessage(
              error,
              'Could not load the shop.',
            ),
          );
        }
      } finally {
        if (!cancelled) {
          setLoading(false);
        }
      }
    }

    loadShopHome();

    return () => {
      cancelled = true;
    };
  }, [
    reloadKey,
    token,
    user?.id,
    user?.shoppingPreference,
  ]);

  const shoppingPreference =
    shopHome?.shoppingPreference ??
    user?.shoppingPreference ??
    null;

    useEffect(() => {
  let cancelled = false;

  async function loadRecentlyViewed() {
    if (!token) {
      return;
    }

    try {
      const data =
        await productService.getRecentlyViewed(
          token,
          12,
        );

      if (!cancelled) {
        setRecentlyViewed(data);
      }
    } catch {
      if (!cancelled) {
        setRecentlyViewed([]);
      }
    }
  }

  loadRecentlyViewed();

  return () => {
    cancelled = true;
  };
}, [token, user?.id]);


  useEffect(() => {
    setActiveFilter('all');
  }, [shoppingPreference]);

  const products =
    shopHome?.products || [];

  const apiCategories =
    shopHome?.categories || [];

  const categoryItems = useMemo(() => {
    if (!shoppingPreference) {
      return [];
    }

    const newInAudience =
      shoppingPreference === 'men'
        ? 'men'
        : 'women';

    return [
      {
        key: 'new-in',
        name: 'New In',
        filter: 'new-in',
        image: getCategoryImage(
          newInAudience,
          'new-in',
        ),
      },

      ...apiCategories.map((category) => ({
        key: `${category.audience}:${category.slug}`,
        name:
          shoppingPreference === 'both'
            ? `${
                category.audience === 'women'
                  ? 'Women'
                  : 'Men'
              } ${category.name}`
            : category.name,

        filter: `category:${category.audience}:${category.slug}`,

        image: getCategoryImage(
          category.audience,
          category.slug,
        ),
      })),
    ];
  }, [
    apiCategories,
    shoppingPreference,
  ]);

  const navigationItems = useMemo(() => {
    if (shoppingPreference === 'both') {
      return [
        {
          label: 'New In',
          filter: 'new-in',
        },
        {
          label: 'Women',
          filter: 'audience:women',
        },
        {
          label: 'Men',
          filter: 'audience:men',
        },
        {
          label: 'Clothing',
          filter: 'clothing',
        },
        {
          label: 'Shoes',
          filter: 'slug:shoes',
        },
        {
          label: 'Accessories',
          filter: 'slug:accessories',
        },
      ];
    }

    return [
      {
        label: 'New In',
        filter: 'new-in',
      },

      ...apiCategories.map((category) => ({
        label: category.name,
        filter: `category:${category.audience}:${category.slug}`,
      })),
    ];
  }, [
    apiCategories,
    shoppingPreference,
  ]);

  const visibleProducts = useMemo(() => {
    const normalizedQuery =
      query.trim().toLowerCase();

    let result = products.filter(
      (product) => {
        let matchesFilter = true;

        if (activeFilter === 'new-in') {
          matchesFilter =
            product.isNew;
        } else if (
          activeFilter === 'saved'
        ) {
          matchesFilter =
            favorites.has(product.id);
        } else if (
          activeFilter === 'clothing'
        ) {
          matchesFilter =
            product.categorySlug !==
              'shoes' &&
            product.categorySlug !==
              'accessories';
        } else if (
          activeFilter.startsWith(
            'audience:',
          )
        ) {
          const audience =
            activeFilter.split(':')[1];

          matchesFilter =
            product.audience === audience;
        } else if (
          activeFilter.startsWith('slug:')
        ) {
          const slug =
            activeFilter.split(':')[1];

          matchesFilter =
            product.categorySlug === slug;
        } else if (
          activeFilter.startsWith(
            'category:',
          )
        ) {
          const [
            ,
            audience,
            slug,
          ] = activeFilter.split(':');

          matchesFilter =
            product.audience === audience &&
            product.categorySlug === slug;
        }

        const matchesQuery =
          !normalizedQuery ||
          [
            product.name,
            product.categoryName,
            product.description,
            product.audience,
          ]
            .join(' ')
            .toLowerCase()
            .includes(normalizedQuery);

        return (
          matchesFilter &&
          matchesQuery
        );
      },
    );

    if (sort === 'price-low') {
      result = [...result].sort(
        (first, second) =>
          Number(first.price) -
          Number(second.price),
      );
    }

    if (sort === 'price-high') {
      result = [...result].sort(
        (first, second) =>
          Number(second.price) -
          Number(first.price),
      );
    }

    if (sort === 'rating') {
      result = [...result].sort(
        (first, second) =>
          Number(second.rating) -
          Number(first.rating),
      );
    }

    if (sort === 'newest') {
      result = [...result].sort(
        (first, second) =>
          Number(second.isNew) -
          Number(first.isNew),
      );
    }

    return result;
  }, [
    activeFilter,
    favorites,
    products,
    query,
    sort,
  ]);

  const displayedProducts =
    visibleProducts.length > 8
      ? visibleProducts.slice(0, 8)
      : visibleProducts;

  const firstName =
    user?.fullName
      ?.trim()
      .split(/\s+/)[0] ||
    'Alaa';

  async function handlePreference(
    preference,
  ) {
    try {
      setSavingPreference(true);
      setPreferenceError('');

      await updateShoppingPreference(
        preference,
      );

      setShopHome(null);
      setLoading(true);
    } catch (error) {
      setPreferenceError(
        getErrorMessage(
          error,
          'Could not save your preference.',
        ),
      );
    } finally {
      setSavingPreference(false);
    }
  }

  async function toggleFavorite(id) {
    const wasFavorite =
      favorites.has(id);

    setFavoriteError('');

    setFavorites((current) => {
      const next = new Set(current);

      if (wasFavorite) {
        next.delete(id);
      } else {
        next.add(id);
      }

      return next;
    });

    try {
      if (wasFavorite) {
        await favoriteService.remove(
          token,
          id,
        );
      } else {
        await favoriteService.add(
          token,
          id,
        );
      }

      setShopHome((current) => {
        if (!current) {
          return current;
        }

        return {
          ...current,
          products: current.products.map(
            (product) =>
              product.id === id
                ? {
                    ...product,
                    isFavorite:
                      !wasFavorite,
                  }
                : product,
          ),
        };
      });
    } catch (error) {
      setFavorites((current) => {
        const next = new Set(current);

        if (wasFavorite) {
          next.add(id);
        } else {
          next.delete(id);
        }

        return next;
      });

      setFavoriteError(
        getErrorMessage(
          error,
          'Could not update your saved items.',
        ),
      );
    }
  }

  function chooseFilter(filter) {
    setActiveFilter(filter);

    document
      .getElementById(
        'shop-recommendations',
      )
      ?.scrollIntoView({
        behavior: 'smooth',
      });
  }

  if (loading && !shopHome) {
    return (
      <main className="preference-screen">
        <section className="preference-card">
          <div className="preference-heading">
            <span className="preference-icon">
              <Sparkles size={25} />
            </span>

            <h1>Preparing your shop</h1>

            <p>
              Loading categories and
              recommendations...
            </p>

            <p className="preference-saving">
              <span className="spinner small" />
              Loading
            </p>
          </div>
        </section>
      </main>
    );
  }

  if (loadError && !shopHome) {
    return (
      <main className="preference-screen">
        <section className="preference-card">
          <div className="preference-heading">
            <span className="preference-icon">
              <Search size={25} />
            </span>

            <h1>Could not load the shop</h1>

            <p>{loadError}</p>
          </div>

          <button
            className="primary-button"
            type="button"
            onClick={() =>
              setReloadKey(
                (current) => current + 1,
              )
            }
          >
            Try again
          </button>
        </section>
      </main>
    );
  }

  if (
    shopHome?.requiresShoppingPreference ||
    !shoppingPreference
  ) {
    return (
      <ShoppingPreferencePrompt
        onSelect={handlePreference}
        saving={savingPreference}
        error={preferenceError}
      />
    );
  }

  return (
    <div className="shop-page">
      <div className="shop-announcement">
        <span>
          <Sparkles size={13} />
          New season styles are here
        </span>

        <span>
          Try first. Choose with confidence.
        </span>
      </div>

      <header className="shop-header">
        <div className="shop-header-main">
          <Link
            className="shop-logo"
            to="/shop"
            aria-label="AI Virtual Try-On home"
          >
            <Sparkles size={19} />

            <span>
              <small>AI VIRTUAL</small>
              TRY-ON
            </span>
          </Link>

          <label className="shop-search">
            <Search
              size={19}
              aria-hidden="true"
            />

            <input
              value={query}
              onChange={(event) =>
                setQuery(
                  event.target.value,
                )
              }
              placeholder="Search clothing, styles and trends"
              aria-label="Search products"
            />
          </label>

          <div className="shop-account-actions">
            <button
              type="button"
              aria-label="Saved items"
              onClick={() =>
                chooseFilter('saved')
              }
            >
              <Heart size={21} />

              <span>
                {favorites.size || ''}
              </span>
            </button>

<NotificationDropdown token={token} />

           <Link
  className="shop-cart-link"
  to="/shop/cart"
  aria-label={`Open shopping bag with ${cartCount} items`}
>
  <ShoppingBag size={21} />

  {cartCount > 0 ? (
    <span className="shop-cart-count">
      {cartCount > 99 ? '99+' : cartCount}
    </span>
  ) : null}
</Link>
            <Link
              className="shop-account"
              to="/profile"
            >
              <span className="shop-avatar">
                {firstName
                  .charAt(0)
                  .toUpperCase()}
              </span>

              <span>{firstName}</span>
              <ChevronDown size={15} />
            </Link>
          </div>
        </div>

        <nav
          className="shop-nav"
          aria-label="Store navigation"
        >
          {navigationItems.map((item) => (
            <button
              className={
                activeFilter === item.filter
                  ? 'is-active'
                  : ''
              }
              type="button"
              key={item.filter}
              onClick={() =>
                chooseFilter(item.filter)
              }
            >
              {item.label}
            </button>
          ))}

          <button
            className="shop-nav-ai"
            type="button"
            disabled
            aria-disabled="true"
            title="AI Virtual Try-On is coming next"
          >
            <Sparkles size={14} />
            Virtual Try-On
          </button>

         <button
  type="button"
  className={
    activeFilter === 'saved'
      ? 'is-active'
      : ''
  }
  onClick={() => chooseFilter('saved')}
>
  My Wardrobe
</button>
        </nav>
      </header>

      <main>
        <section className="shop-hero">
          <img
            src={heroBackground}
            alt="Models wearing contemporary neutral outfits"
          />

          <div className="shop-hero-shade" />

          <div className="shop-hero-copy">
            <span className="shop-eyebrow">
              AI Virtual Try-On
            </span>

            <h1>
              Your Style. Your Fit.
              <br />
              <em>Virtually Yours.</em>
            </h1>

            <p>
              Discover pieces selected for your
              taste and see the fit before you
              choose.
            </p>

            <div>
              <button
                type="button"
                onClick={() =>
                  chooseFilter('new-in')
                }
              >
                Shop New Arrivals
              </button>

              <button
                className="shop-secondary-action"
                type="button"
                disabled
                aria-disabled="true"
                title="AI Virtual Try-On is coming next"
              >
                <Sparkles size={16} />
                Start Virtual Try-On
              </button>
            </div>
          </div>

          <aside className="shop-fit-callout">
            <Sparkles size={25} />

            <strong>
              AI FIT
              <br />
              PERFECTED
            </strong>

            <span>
              Better fit.
              <br />
              More confidence.
              <br />
              Every time.
            </span>
          </aside>
        </section>

        <section
          className="shop-categories"
          aria-label="Shop by category"
        >
          {categoryItems.map((category) => (
            <button
              className={
                activeFilter ===
                category.filter
                  ? 'is-active'
                  : ''
              }
              type="button"
              key={category.key}
              onClick={() =>
                chooseFilter(
                  category.filter,
                )
              }
            >
              <span>
                <img
                  src={category.image}
                  alt=""
                />
              </span>

              <strong>
                {category.name}
              </strong>
            </button>
          ))}

          <button
            className={
              activeFilter === 'saved'
                ? 'is-active'
                : ''
            }
            type="button"
            onClick={() =>
              chooseFilter('saved')
            }
          >
            <span className="shop-saved-category">
              <Heart size={28} />
            </span>

            <strong>Saved</strong>
          </button>
        </section>

        <section
          className="shop-recommendations"
          id="shop-recommendations"
        >
          <div className="shop-section-heading">
            <div>
              <span className="shop-eyebrow">
                Picked for your profile
              </span>

              <h2>
                Recommended for You,{' '}
                {firstName}
              </h2>
            </div>

            <div className="shop-filters">
              <label>
                <span className="sr-only">
                  Sort products
                </span>

                <select
                  value={sort}
                  onChange={(event) =>
                    setSort(
                      event.target.value,
                    )
                  }
                >
                  <option value="recommended">
                    Sort: Recommended
                  </option>

                  <option value="newest">
                    New arrivals
                  </option>

                  <option value="price-low">
                    Price: Low to high
                  </option>

                  <option value="price-high">
                    Price: High to low
                  </option>

                  <option value="rating">
                    Highest rated
                  </option>
                </select>
              </label>
            </div>
          </div>

          {favoriteError ? (
            <div
              className="alert alert-error"
              role="alert"
            >
              {favoriteError}
            </div>
          ) : null}

          {visibleProducts.length ? (
            <div className="shop-product-grid">
              {displayedProducts.map(
                (product) => (
                  <ProductCard
                    key={product.id}
                    product={product}
                    favorite={favorites.has(
                      product.id,
                    )}
                    onFavorite={
                      toggleFavorite
                    }
                  />
                ),
              )}
            </div>
          ) : (
            <div className="shop-empty-state">
              <Search size={28} />

              <h3>No matching styles</h3>

              <p>
                Try another search or
                category.
              </p>

              <button
                type="button"
                onClick={() => {
                  setQuery('');
                  setActiveFilter('all');
                }}
              >
                View all products
              </button>
            </div>
          )}
        </section>

        <section className="shop-recently-viewed">
          <div className="shop-section-heading">
            <div>
              <span className="shop-eyebrow">
                From your catalog
              </span>

              <h2>Recently Viewed</h2>
            </div>

           {recentlyViewed.length > 6 ? (
  <button
    type="button"
    onClick={() =>
      setShowAllRecently((current) => !current)
    }
  >
    {showAllRecently ? 'Show less' : 'View all'}
  </button>
) : null}
          </div>

          <div className="shop-recent-row">
  {recentlyViewed.length ? (
    recentlyViewed
      .slice(
        0,
        showAllRecently
          ? recentlyViewed.length
          : 6,
      )
      .map((product) => (
        <Link
          key={product.id}
          to={`/shop/products/${product.id}`}
        >
          <img
            src={product.imageUrl}
            alt={product.name}
            loading="lazy"
          />

          <span>{product.name}</span>
        </Link>
      ))
  ) : (
    <p className="shop-recent-empty">
      Products you open will appear here.
    </p>
  )}
</div>
        </section>

        <section
          className="shop-benefits"
          aria-label="Shopping benefits"
        >
          <div>
            <Sparkles />

            <span>
              <strong>AI Perfect Fit</strong>

              <small>
                Smart size recommendations
              </small>
            </span>
          </div>

          <div>
            <UserRound />

            <span>
              <strong>
                Style, Personalized
              </strong>

              <small>
                AI picks made for you
              </small>
            </span>
          </div>

          <div>
            <ShoppingBag />

            <span>
              <strong>
                Try Before You Choose
              </strong>

              <small>
                See it on you, risk-free
              </small>
            </span>
          </div>

          <div>
            <Heart />

            <span>
              <strong>
                Save Your Favorites
              </strong>

              <small>
                Build a wardrobe you love
              </small>
            </span>
          </div>
        </section>
      </main>
    </div>
  );
}
