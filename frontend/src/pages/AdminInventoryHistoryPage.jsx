import {
  useCallback,
  useEffect,
  useMemo,
  useState,
} from 'react';

import {
  Boxes,
  Check,
  History,
  Minus,
  PackagePlus,
  Plus,
  RefreshCw,
  Search,
  SlidersHorizontal,
  X,
} from 'lucide-react';

import AdminSidebar from '../components/admin/AdminSidebar.jsx';
import { useAuth } from '../context/AuthContext.jsx';
import { getErrorMessage } from '../services/authService.js';
import { inventoryService } from '../services/inventoryService.js';

import './AdminProductsPage.css';
import './AdminManagement.css';
import './AdminInventoryHistoryPage.css';

const emptyDraft = {
  variantId: '',
  operation: 'Add',
  quantity: 1,
  reason: '',
};

function resultingStock(variant, draft) {
  if (!variant) {
    return 0;
  }

  const quantity = Number(draft.quantity || 0);

  if (draft.operation === 'Add') {
    return variant.stockQuantity + quantity;
  }

  if (draft.operation === 'Remove') {
    return variant.stockQuantity - quantity;
  }

  return quantity;
}

export default function AdminInventoryHistoryPage() {
  const { token, user } = useAuth();

  const [variants, setVariants] = useState([]);
  const [history, setHistory] = useState([]);
  const [query, setQuery] = useState('');
  const [stockFilter, setStockFilter] = useState('All');
  const [draft, setDraft] = useState(null);
  const [loading, setLoading] = useState(true);
  const [synchronizing, setSynchronizing] = useState(false);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState('');
  const [success, setSuccess] = useState('');

  const load = useCallback(async () => {
    try {
      setLoading(true);
      setError('');

      let [variantData, historyData] = await Promise.all([
        inventoryService.getVariants(token),
        inventoryService.getHistory(token),
      ]);

      if (variantData.length === 0) {
        await inventoryService.synchronize(token);

        variantData = await inventoryService.getVariants(token);
      }

      setVariants(variantData);
      setHistory(historyData);
    } catch (loadError) {
      setError(
        getErrorMessage(
          loadError,
          'Inventory could not be loaded.'
        )
      );
    } finally {
      setLoading(false);
    }
  }, [token]);

  useEffect(() => {
    load();
  }, [load]);

  const visibleVariants = useMemo(() => {
    const normalizedQuery = query.trim().toLowerCase();

    return variants.filter((variant) => {
      const matchesQuery =
        `${variant.productName} ${variant.categoryName} ` +
        `${variant.colorName} ${variant.sizeName} ${variant.sku}`
          .toLowerCase()
          .includes(normalizedQuery);

      const matchesStock =
        stockFilter === 'All' ||
        (stockFilter === 'Low' && variant.isLowStock) ||
        (stockFilter === 'Out' && variant.stockQuantity === 0) ||
        (stockFilter === 'Available' &&
          variant.stockQuantity > 0);

      return matchesQuery && matchesStock;
    });
  }, [variants, query, stockFilter]);

  const selectedVariant = useMemo(
    () =>
      variants.find(
        (variant) => variant.id === draft?.variantId
      ),
    [variants, draft?.variantId]
  );

  const previewQuantity = draft
    ? resultingStock(selectedVariant, draft)
    : 0;

  function openAdjustment(variant) {
    setError('');
    setSuccess('');

    setDraft({
      ...emptyDraft,
      variantId: variant.id,
    });
  }

  async function synchronize() {
    try {
      setSynchronizing(true);
      setError('');
      setSuccess('');

      const result = await inventoryService.synchronize(token);

      setSuccess(
        result.createdCount > 0
          ? `${result.createdCount} new variant(s) created.`
          : 'All product variants are already synchronized.'
      );

      await load();
    } catch (syncError) {
      setError(
        getErrorMessage(
          syncError,
          'Variants could not be synchronized.'
        )
      );
    } finally {
      setSynchronizing(false);
    }
  }

  async function saveAdjustment(event) {
    event.preventDefault();

    if (!selectedVariant) {
      setError('Select an exact product variant.');
      return;
    }

    if (
      draft.operation === 'Remove' &&
      previewQuantity < 0
    ) {
      setError(
        `Only ${selectedVariant.stockQuantity} item(s) are available.`
      );
      return;
    }

    try {
      setSaving(true);
      setError('');
      setSuccess('');

      const adjustment = await inventoryService.adjust(
        token,
        selectedVariant.id,
        {
          operation: draft.operation,
          quantity: Number(draft.quantity),
          reason: draft.reason.trim(),
        }
      );

      setVariants((current) =>
        current.map((variant) =>
          variant.id === selectedVariant.id
            ? {
                ...variant,
                stockQuantity: adjustment.newQuantity,
                isLowStock:
                  adjustment.newQuantity <=
                  variant.lowStockThreshold,
              }
            : variant
        )
      );

      setHistory((current) => [
        adjustment,
        ...current,
      ]);

      setSuccess(
        `${selectedVariant.productName} / ` +
        `${selectedVariant.colorName} / ` +
        `${selectedVariant.sizeName} updated to ` +
        `${adjustment.newQuantity}.`
      );

      setDraft(null);
    } catch (saveError) {
      setError(
        getErrorMessage(
          saveError,
          'Stock could not be adjusted.'
        )
      );
    } finally {
      setSaving(false);
    }
  }

  return (
    <div className="admin-shell">
      <AdminSidebar user={user} />

      <main className="admin-main">
        <header className="admin-topbar">
          <div>
            <span>Inventory</span>
            <b>/</b>
            <strong>Variant stock</strong>
          </div>
        </header>

        <div className="admin-content inventory-content">
          <section className="admin-page-title">
            <div>
              <span className="admin-eyebrow">
                EXACT STOCK CONTROL
              </span>

              <h1>Inventory</h1>

              <p>
                Manage stock by product, color, size,
                and SKU.
              </p>
            </div>

            <button
              type="button"
              className="admin-secondary-btn"
              onClick={synchronize}
              disabled={synchronizing}
            >
              <RefreshCw
                size={16}
                className={
                  synchronizing ? 'spin' : ''
                }
              />

              {synchronizing
                ? 'Synchronizing…'
                : 'Synchronize variants'}
            </button>
          </section>

          {error ? (
            <div className="admin-notice">
              {error}
            </div>
          ) : null}

          {success ? (
            <div className="inventory-success">
              <Check size={17} />
              {success}
            </div>
          ) : null}

          <section className="inventory-summary">
            <article>
              <Boxes size={20} />
              <span>
                <small>Total variants</small>
                <strong>{variants.length}</strong>
              </span>
            </article>

            <article>
              <PackagePlus size={20} />
              <span>
                <small>Units available</small>
                <strong>
                  {variants.reduce(
                    (total, variant) =>
                      total + variant.stockQuantity,
                    0
                  )}
                </strong>
              </span>
            </article>

            <article>
              <SlidersHorizontal size={20} />
              <span>
                <small>Low stock</small>
                <strong>
                  {
                    variants.filter(
                      (variant) => variant.isLowStock
                    ).length
                  }
                </strong>
              </span>
            </article>
          </section>

          <section className="inventory-panel">
            <div className="inventory-toolbar">
              <div>
                <h2>Product variants</h2>
                <p>
                  {visibleVariants.length} exact items
                </p>
              </div>

              <div className="inventory-filters">
                <label>
                  <Search size={16} />

                  <input
                    value={query}
                    onChange={(event) =>
                      setQuery(event.target.value)
                    }
                    placeholder="Product, color, size or SKU"
                  />
                </label>

                <select
                  value={stockFilter}
                  onChange={(event) =>
                    setStockFilter(event.target.value)
                  }
                >
                  <option>All</option>
                  <option>Available</option>
                  <option>Low</option>
                  <option>Out</option>
                </select>
              </div>
            </div>

            {loading ? (
              <div className="admin-empty">
                Loading exact inventory…
              </div>
            ) : visibleVariants.length === 0 ? (
              <div className="admin-empty">
                No variants match this view.
              </div>
            ) : (
              <div className="inventory-variant-grid">
                {visibleVariants.map((variant) => (
                  <article
                    className="inventory-variant-card"
                    key={variant.id}
                  >
                    <img
                      src={
                        variant.colorImageUrl ||
                        variant.productImageUrl
                      }
                      alt=""
                    />

                    <div className="inventory-variant-info">
                      <span className="inventory-category">
                        {variant.categoryName}
                      </span>

                      <h3>{variant.productName}</h3>

                      <div className="inventory-identity">
                        <span>
                          <i
                            style={{
                              background:
                                variant.colorHexCode,
                            }}
                          />

                          {variant.colorName}
                        </span>

                        <span>
                          Size {variant.sizeName}
                        </span>
                      </div>

                      <code>{variant.sku}</code>
                    </div>

                    <div className="inventory-stock">
                      <small>Current stock</small>

                      <strong
                        className={
                          variant.stockQuantity === 0
                            ? 'out'
                            : variant.isLowStock
                              ? 'low'
                              : ''
                        }
                      >
                        {variant.stockQuantity}
                      </strong>

                      <span>
                        {variant.stockQuantity === 0
                          ? 'Out of stock'
                          : variant.isLowStock
                            ? 'Low stock'
                            : 'Available'}
                      </span>
                    </div>

                    <button
                      type="button"
                      onClick={() =>
                        openAdjustment(variant)
                      }
                    >
                      Adjust stock
                    </button>
                  </article>
                ))}
              </div>
            )}
          </section>

          <section className="inventory-panel">
            <div className="inventory-toolbar">
              <div>
                <h2>Adjustment history</h2>
                <p>
                  Every manual stock change is recorded
                </p>
              </div>

              <History size={20} />
            </div>

            <div className="admin-table-wrap">
              <table className="admin-products-table">
                <thead>
                  <tr>
                    <th>Exact item</th>
                    <th>Operation</th>
                    <th>Stock change</th>
                    <th>Reason</th>
                    <th>Administrator</th>
                    <th>Date</th>
                  </tr>
                </thead>

                <tbody>
                  {history.length === 0 ? (
                    <tr>
                      <td
                        colSpan="6"
                        className="admin-empty"
                      >
                        No variant adjustments recorded.
                      </td>
                    </tr>
                  ) : (
                    history.map((item) => (
                      <tr key={item.id}>
                        <td>
                          <strong>
                            {item.productName}
                          </strong>

                          <small>
                            {item.colorName} /{' '}
                            {item.sizeName}
                          </small>

                          <code>{item.sku}</code>
                        </td>

                        <td>
                          <strong>
                            {item.operation}
                          </strong>

                          <small>
                            Quantity{' '}
                            {item.enteredQuantity}
                          </small>
                        </td>

                        <td>
                          <strong>
                            {item.previousQuantity}
                            {' → '}
                            {item.newQuantity}
                          </strong>

                          <small>
                            {item.difference > 0
                              ? '+'
                              : ''}
                            {item.difference}
                          </small>
                        </td>

                        <td>
                          <strong>{item.reason}</strong>
                        </td>

                        <td>
                          <strong>
                            {item.administratorName}
                          </strong>

                          <small>
                            {item.administratorEmail}
                          </small>
                        </td>

                        <td>
                          <strong>
                            {new Date(
                              item.createdAt
                            ).toLocaleDateString()}
                          </strong>

                          <small>
                            {new Date(
                              item.createdAt
                            ).toLocaleTimeString()}
                          </small>
                        </td>
                      </tr>
                    ))
                  )}
                </tbody>
              </table>
            </div>
          </section>
        </div>
      </main>

      {draft && selectedVariant ? (
        <div
          className="management-modal"
          onMouseDown={() => setDraft(null)}
        >
          <form
            className="inventory-adjustment-modal"
            onSubmit={saveAdjustment}
            onMouseDown={(event) =>
              event.stopPropagation()
            }
          >
            <button
              type="button"
              className="modal-close"
              onClick={() => setDraft(null)}
              aria-label="Close"
            >
              <X size={18} />
            </button>

            <span className="admin-eyebrow">
              EXACT ITEM
            </span>

            <h2>Adjust stock</h2>

            <div className="selected-variant">
              <img
                src={
                  selectedVariant.colorImageUrl ||
                  selectedVariant.productImageUrl
                }
                alt=""
              />

              <div>
                <strong>
                  {selectedVariant.productName}
                </strong>

                <span>
                  <i
                    style={{
                      background:
                        selectedVariant.colorHexCode,
                    }}
                  />

                  {selectedVariant.colorName}
                  {' / '}
                  Size {selectedVariant.sizeName}
                </span>

                <code>{selectedVariant.sku}</code>
              </div>
            </div>

            <div className="current-stock-line">
              <span>Current stock</span>
              <strong>
                {selectedVariant.stockQuantity}
              </strong>
            </div>

            <label>
              Adjustment type

              <div className="operation-options">
                {[
                  ['Add', Plus],
                  ['Remove', Minus],
                  ['Set', SlidersHorizontal],
                ].map(([operation, Icon]) => (
                  <button
                    key={operation}
                    type="button"
                    className={
                      draft.operation === operation
                        ? 'selected'
                        : ''
                    }
                    onClick={() =>
                      setDraft({
                        ...draft,
                        operation,
                      })
                    }
                  >
                    <Icon size={16} />
                    {operation}
                  </button>
                ))}
              </div>
            </label>

            <label>
              Quantity

              <input
                required
                type="number"
                min={
                  draft.operation === 'Set'
                    ? 0
                    : 1
                }
                value={draft.quantity}
                onChange={(event) =>
                  setDraft({
                    ...draft,
                    quantity: event.target.value,
                  })
                }
              />
            </label>

            <div
              className={`stock-preview ${
                previewQuantity < 0 ? 'invalid' : ''
              }`}
            >
              <span>Resulting stock</span>
              <strong>{previewQuantity}</strong>
            </div>

            <label>
              Reason

              <textarea
                required
                minLength="3"
                maxLength="300"
                rows="4"
                value={draft.reason}
                onChange={(event) =>
                  setDraft({
                    ...draft,
                    reason: event.target.value,
                  })
                }
                placeholder="Warehouse delivery, damaged item, stock count correction…"
              />
            </label>

            <button
              className="admin-primary-btn"
              disabled={
                saving || previewQuantity < 0
              }
            >
              {saving
                ? 'Saving adjustment…'
                : 'Confirm stock adjustment'}
            </button>
          </form>
        </div>
      ) : null}
    </div>
  );
}

