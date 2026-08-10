import { useEffect, useState } from 'react';
import { Link, useParams } from 'react-router-dom';
import {
  ArrowLeft,
  Heart,
  Minus,
  Plus,
  ShoppingBag,
  Sparkles,
  Star,
} from 'lucide-react';

import { useAuth } from '../context/AuthContext.jsx';
import { cartService } from '../services/cartService.js';
import { favoriteService } from '../services/favoriteService.js';
import { productService } from '../services/productService.js';
import { getErrorMessage } from '../services/authService.js';
import ProductReviews from '../components/shop/ProductReviews.jsx';

export default function ProductDetailsPage() {
  const { productId } = useParams();
  const { token } = useAuth();

  const [product, setProduct] = useState(null);
  const [selectedColorId, setSelectedColorId] =
    useState('');
  const [selectedSizeId, setSelectedSizeId] =
    useState('');
  const [quantity, setQuantity] = useState(1);
  const [loading, setLoading] = useState(true);
  const [addingToCart, setAddingToCart] =
    useState(false);
  const [updatingFavorite, setUpdatingFavorite] =
    useState(false);
  const [error, setError] = useState('');
  const [favoriteError, setFavoriteError] =
    useState('');
  const [cartError, setCartError] = useState('');
  const [cartMessage, setCartMessage] =
    useState('');

  useEffect(() => {
    let cancelled = false;

    async function loadProduct() {
      if (!token || !productId) {
        return;
      }

      try {
        setLoading(true);
        setError('');
        setCartError('');
        setCartMessage('');
        setQuantity(1);

        const data = await productService.getById(
          token,
          productId,
        );

        if (!cancelled) {
          setProduct(data);

          const firstColor = data.colors?.[0];

          const firstAvailableSize =
            data.sizes?.find(
              (size) => size.stockQuantity > 0,
            );

          setSelectedColorId(firstColor?.id || '');

          setSelectedSizeId(
            firstAvailableSize?.id || '',
          );
        }
      } catch (requestError) {
        if (!cancelled) {
          setError(
            getErrorMessage(
              requestError,
              'Could not load this product.',
            ),
          );
        }
      } finally {
        if (!cancelled) {
          setLoading(false);
        }
      }
    }

    loadProduct();

    return () => {
      cancelled = true;
    };
  }, [productId, token]);

  async function toggleFavorite() {
    if (!product || updatingFavorite) {
      return;
    }

    const wasFavorite = product.isFavorite;

    setFavoriteError('');
    setUpdatingFavorite(true);

    setProduct((current) => ({
      ...current,
      isFavorite: !wasFavorite,
    }));

    try {
      if (wasFavorite) {
        await favoriteService.remove(
          token,
          product.id,
        );
      } else {
        await favoriteService.add(
          token,
          product.id,
        );
      }
    } catch (requestError) {
      setProduct((current) => ({
        ...current,
        isFavorite: wasFavorite,
      }));

      setFavoriteError(
        getErrorMessage(
          requestError,
          'Could not update your saved items.',
        ),
      );
    } finally {
      setUpdatingFavorite(false);
    }
  }

  function chooseColor(colorId) {
    setSelectedColorId(colorId);
    setCartError('');
    setCartMessage('');
  }

  function chooseSize(size) {
    if (size.stockQuantity <= 0) {
      return;
    }

    setSelectedSizeId(size.id);

    setQuantity((current) =>
      Math.min(
        current,
        size.stockQuantity,
        20,
      ),
    );

    setCartError('');
    setCartMessage('');
  }

 async function addToCart() {
  if (!selectedColorId) {
    setCartError('Please select a color.');
    return;
  }

  if (!selectedSizeId) {
    setCartError(
      'Please select an available size.',
    );
    return;
  }

  try {
    setAddingToCart(true);
    setCartError('');
    setCartMessage('');

    const cart = await cartService.add(
      token,
      selectedSizeId,
      selectedColorId,
      quantity,
    );

    setCartMessage(
      `${product.name} was added to your bag. ` +
        `Your bag now contains ${cart.totalQuantity} ` +
        `${cart.totalQuantity === 1 ? 'item' : 'items'}.`,
    );

    window.dispatchEvent(
      new CustomEvent('cart-updated', {
        detail: cart.totalQuantity,
      }),
    );

    const chosenColor =
      product.colors?.find(
        (color) =>
          color.id === selectedColorId,
      );

    const chosenSize =
      product.sizes?.find(
        (size) =>
          size.id === selectedSizeId,
      );

    window.dispatchEvent(
      new CustomEvent('app-notification', {
        detail: {
          title: 'Added to your bag',
          message:
            `${product.name}, ` +
            `${chosenColor?.name || 'selected color'}, ` +
            `size ${chosenSize?.name || 'selected size'}.`,
          link: '/shop/cart',
        },
      }),
    );
  } catch (requestError) {
    setCartError(
      getErrorMessage(
        requestError,
        'Could not add this product to your bag.',
      ),
    );
  } finally {
    setAddingToCart(false);
  }
}


  if (loading) {
    return (
      <main className="product-details-state">
        <span className="spinner" />
        <p>Loading product...</p>
      </main>
    );
  }

  if (error || !product) {
    return (
      <main className="product-details-state">
        <h1>Product unavailable</h1>

        <p>
          {error ||
            'This product does not exist.'}
        </p>

        <Link to="/shop">
          <ArrowLeft size={18} />
          Back to shop
        </Link>
      </main>
    );
  }

  const availableSizes =
    product.sizes?.filter(
      (size) => size.stockQuantity > 0,
    ) || [];

  const selectedSize = availableSizes.find(
    (size) => size.id === selectedSizeId,
  );

  const selectedColor = product.colors?.find(
    (color) => color.id === selectedColorId,
  );
const displayedImage =
  selectedColor?.imageUrl || product.imageUrl;

  const maximumQuantity = Math.min(
    selectedSize?.stockQuantity || 1,
    20,
  );

  return (
    <main className="product-details-page">
      <Link
        className="product-details-back"
        to="/shop"
      >
        <ArrowLeft size={18} />
        Back to shop
      </Link>

      <section className="product-details-layout">
        <div className="product-details-image">
         <img
  src={displayedImage}
  alt={
    selectedColor
      ? `${product.name} in ${selectedColor.name}`
      : product.name
  }
/>

          {product.badge ? (
            <span>{product.badge}</span>
          ) : null}
        </div>

        <div className="product-details-content">
          <p className="product-details-category">
            {product.categoryName}
          </p>

          <div className="product-details-heading">
            <h1>{product.name}</h1>

            <button
              type="button"
              className={
                product.isFavorite
                  ? 'is-favorite'
                  : ''
              }
              onClick={toggleFavorite}
              disabled={updatingFavorite}
              aria-label={
                product.isFavorite
                  ? 'Remove from saved items'
                  : 'Add to saved items'
              }
            >
              <Heart
                size={24}
                fill={
                  product.isFavorite
                    ? 'currentColor'
                    : 'none'
                }
              />
            </button>
          </div>

          <div className="product-details-rating">
            <Star size={17} fill="currentColor" />
            <strong>{product.rating}</strong>

            <span>
              ({product.reviewCount} reviews)
            </span>
          </div>

          <strong className="product-details-price">
            ${Number(product.price).toFixed(2)}
          </strong>

          <p className="product-details-description">
            {product.description}
          </p>

          <div className="product-details-option">
            <div className="product-option-heading">
              <h2>Select color</h2>

              {selectedColor ? (
                <span>{selectedColor.name}</span>
              ) : null}
            </div>

            <div className="product-details-colors">
              {product.colors?.map((color) => (
                <button
                  key={color.id}
                  type="button"
                  className={
                    selectedColorId === color.id
                      ? 'is-selected'
                      : ''
                  }
                  title={color.name}
                  aria-label={`Select ${color.name}`}
                  aria-pressed={
                    selectedColorId === color.id
                  }
                  onClick={() =>
                    chooseColor(color.id)
                  }
                >
                  <span
                    style={{
                      backgroundColor:
                        color.hexCode,
                    }}
                  />
                </button>
              ))}
            </div>
          </div>

          <div className="product-details-option">
            <h2>Select size</h2>

            <div className="product-details-sizes">
              {product.sizes?.map((size) => (
                <button
                  key={size.id}
                  type="button"
                  className={
                    selectedSizeId === size.id
                      ? 'is-selected'
                      : ''
                  }
                  disabled={
                    size.stockQuantity <= 0
                  }
                  onClick={() => chooseSize(size)}
                  aria-pressed={
                    selectedSizeId === size.id
                  }
                >
                  {size.name}
                </button>
              ))}
            </div>

            {selectedSize ? (
              <small className="product-stock-message">
                {selectedSize.stockQuantity} available
              </small>
            ) : null}
          </div>

          <div className="product-details-purchase">
            <div className="product-quantity">
              <span>Quantity</span>

              <div>
                <button
                  type="button"
                  onClick={() =>
                    setQuantity((current) =>
                      Math.max(1, current - 1),
                    )
                  }
                  disabled={quantity <= 1}
                  aria-label="Decrease quantity"
                >
                  <Minus size={15} />
                </button>

                <strong>{quantity}</strong>

                <button
                  type="button"
                  onClick={() =>
                    setQuantity((current) =>
                      Math.min(
                        maximumQuantity,
                        current + 1,
                      ),
                    )
                  }
                  disabled={
                    quantity >= maximumQuantity
                  }
                  aria-label="Increase quantity"
                >
                  <Plus size={15} />
                </button>
              </div>
            </div>

            <button
              className="product-add-to-cart"
              type="button"
              onClick={addToCart}
              disabled={
                !selectedColorId ||
                !selectedSizeId ||
                addingToCart
              }
            >
              {addingToCart ? (
                <span className="spinner small" />
              ) : (
                <ShoppingBag size={18} />
              )}

              {addingToCart
                ? 'Adding...'
                : 'Add to Bag'}
            </button>
          </div>

          {cartMessage ? (
            <div
              className="alert alert-success"
              role="status"
            >
              {cartMessage}
            </div>
          ) : null}

          {cartError ? (
            <div
              className="alert alert-error"
              role="alert"
            >
              {cartError}
            </div>
          ) : null}

          {favoriteError ? (
            <div
              className="alert alert-error"
              role="alert"
            >
              {favoriteError}
            </div>
          ) : null}

          <button
            className="product-details-try"
            type="button"
            disabled
            title="AI Virtual Try-On is coming next"
          >
            <Sparkles size={18} />
            Start Virtual Try-On
          </button>
        </div>
      </section>

      <ProductReviews
        productId={product.id}
        token={token}
        onSummaryChange={(summary) =>
          setProduct((current) => ({
            ...current,
            rating: summary.averageRating,
            reviewCount: summary.reviewCount,
          }))
        }
      />
    </main>
  );
}