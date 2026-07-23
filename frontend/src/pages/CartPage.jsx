import { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import {
  ArrowLeft,
  Minus,
  Plus,
  ShieldCheck,
  ShoppingBag,
  Trash2,
} from 'lucide-react';

import { useAuth } from '../context/AuthContext.jsx';
import { cartService } from '../services/cartService.js';
import { getErrorMessage } from '../services/authService.js';

const emptyCart = {
  items: [],
  totalQuantity: 0,
  subtotal: 0,
};

export default function CartPage() {
  const { token } = useAuth();

  const [cart, setCart] = useState(emptyCart);
  const [loading, setLoading] = useState(true);
  const [busyItemId, setBusyItemId] =
    useState('');
  const [clearing, setClearing] =
    useState(false);
  const [error, setError] = useState('');

  useEffect(() => {
    let cancelled = false;

    async function loadCart() {
      if (!token) {
        return;
      }

      try {
        setLoading(true);
        setError('');

        const data = await cartService.get(token);

        if (!cancelled) {
          setCart(data);
        }
      } catch (requestError) {
        if (!cancelled) {
          setError(
            getErrorMessage(
              requestError,
              'Could not load your shopping bag.',
            ),
          );
        }
      } finally {
        if (!cancelled) {
          setLoading(false);
        }
      }
    }

    loadCart();

    return () => {
      cancelled = true;
    };
  }, [token]);

  async function updateQuantity(item, quantity) {
    if (
      quantity < 1 ||
      quantity > item.stockQuantity ||
      quantity > 20
    ) {
      return;
    }

    try {
      setBusyItemId(item.id);
      setError('');

      const data = await cartService.update(
        token,
        item.id,
        quantity,
      );

      setCart(data);
    } catch (requestError) {
      setError(
        getErrorMessage(
          requestError,
          'Could not update this item.',
        ),
      );
    } finally {
      setBusyItemId('');
    }
  }

  async function removeItem(itemId) {
    try {
      setBusyItemId(itemId);
      setError('');

      const data = await cartService.remove(
        token,
        itemId,
      );

      setCart(data);
    } catch (requestError) {
      setError(
        getErrorMessage(
          requestError,
          'Could not remove this item.',
        ),
      );
    } finally {
      setBusyItemId('');
    }
  }

  async function clearCart() {
    try {
      setClearing(true);
      setError('');

      const data = await cartService.clear(token);
      setCart(data);
    } catch (requestError) {
      setError(
        getErrorMessage(
          requestError,
          'Could not clear your shopping bag.',
        ),
      );
    } finally {
      setClearing(false);
    }
  }

  if (loading) {
    return (
      <main className="cart-state">
        <span className="spinner" />
        <p>Loading your bag...</p>
      </main>
    );
  }

  if (!cart.items.length) {
    return (
      <main className="cart-page">
        <Link className="cart-back" to="/shop">
          <ArrowLeft size={18} />
          Continue shopping
        </Link>

        <section className="cart-empty">
          <span>
            <ShoppingBag size={42} />
          </span>

          <p className="cart-eyebrow">
            Your shopping bag
          </p>

          <h1>Your bag is empty</h1>

          <p>
            Discover pieces selected for your style
            and add your favorites here.
          </p>

          <Link to="/shop">
            Explore the collection
          </Link>
        </section>
      </main>
    );
  }

  return (
    <main className="cart-page">
      <Link className="cart-back" to="/shop">
        <ArrowLeft size={18} />
        Continue shopping
      </Link>

      <div className="cart-heading">
        <div>
          <p className="cart-eyebrow">
            Selected for you
          </p>

          <h1>Shopping Bag</h1>

          <span>
            {cart.totalQuantity}{' '}
            {cart.totalQuantity === 1
              ? 'item'
              : 'items'}
          </span>
        </div>

        <button
          type="button"
          onClick={clearCart}
          disabled={clearing}
        >
          <Trash2 size={15} />
          {clearing ? 'Clearing...' : 'Clear bag'}
        </button>
      </div>

      {error ? (
        <div
          className="alert alert-error cart-error"
          role="alert"
        >
          {error}
        </div>
      ) : null}

      <div className="cart-layout">
        <section
          className="cart-items"
          aria-label="Shopping bag items"
        >
          {cart.items.map((item) => {
            const busy = busyItemId === item.id;

            return (
              <article
                className="cart-item"
                key={item.id}
              >
                <Link
                  className="cart-item-image"
                  to={`/shop/products/${item.productId}`}
                >
                  <img
                    src={item.imageUrl}
                    alt={item.productName}
                  />
                </Link>

                <div className="cart-item-content">
                  <div className="cart-item-top">
                    <div>
                      <p>{item.categoryName}</p>

                      <h2>
                        <Link
                          to={`/shop/products/${item.productId}`}
                        >
                          {item.productName}
                        </Link>
                      </h2>

                      <div className="cart-item-variants">
  <span>
    Size: <strong>{item.size}</strong>
  </span>

  {item.colorName ? (
    <span>
      Color:

      <i
        className="cart-color-swatch"
        style={{
          backgroundColor:
            item.colorHexCode || '#ffffff',
        }}
        aria-hidden="true"
      />

      <strong>{item.colorName}</strong>
    </span>
  ) : null}
</div>
                    </div>

                    <strong>
                      $
                      {Number(item.lineTotal).toFixed(2)}
                    </strong>
                  </div>

                  <div className="cart-item-bottom">
                    <div className="cart-item-quantity">
                      <button
                        type="button"
                        aria-label="Decrease quantity"
                        disabled={
                          busy || item.quantity <= 1
                        }
                        onClick={() =>
                          updateQuantity(
                            item,
                            item.quantity - 1,
                          )
                        }
                      >
                        <Minus size={14} />
                      </button>

                      <span>
                        {busy ? '...' : item.quantity}
                      </span>

                      <button
                        type="button"
                        aria-label="Increase quantity"
                        disabled={
                          busy ||
                          item.quantity >=
                            item.stockQuantity ||
                          item.quantity >= 20
                        }
                        onClick={() =>
                          updateQuantity(
                            item,
                            item.quantity + 1,
                          )
                        }
                      >
                        <Plus size={14} />
                      </button>
                    </div>

                    <button
                      className="cart-remove"
                      type="button"
                      disabled={busy}
                      onClick={() =>
                        removeItem(item.id)
                      }
                    >
                      <Trash2 size={15} />
                      Remove
                    </button>
                  </div>
                </div>
              </article>
            );
          })}
        </section>

        <aside className="cart-summary">
          <p className="cart-eyebrow">
            Order summary
          </p>

          <h2>Summary</h2>

          <div>
            <span>
              Items ({cart.totalQuantity})
            </span>

            <strong>
              ${Number(cart.subtotal).toFixed(2)}
            </strong>
          </div>

          <div>
            <span>Delivery</span>
            <strong>Calculated later</strong>
          </div>

          <div className="cart-summary-total">
            <span>Subtotal</span>

            <strong>
              ${Number(cart.subtotal).toFixed(2)}
            </strong>
          </div>

          <p className="cart-summary-note">
            Taxes and delivery are calculated during
            checkout.
          </p>

          <Link
            className="cart-summary-shopping"
            to="/shop"
          >
            Continue Shopping
          </Link>

          <div className="cart-secure">
            <ShieldCheck size={19} />

            <span>
              <strong>Secure shopping</strong>
              <small>
                Your account and cart are protected.
              </small>
            </span>
          </div>
        </aside>
      </div>
    </main>
  );
}