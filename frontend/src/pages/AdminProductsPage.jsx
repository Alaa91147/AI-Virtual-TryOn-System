import { useEffect, useMemo, useRef, useState } from 'react';
import {
  Archive, ArrowLeft, Box, Check, ChevronDown, CircleDollarSign,
  Eye, ImagePlus, LayoutDashboard, MoreHorizontal, Package,
  Pencil, Plus, RefreshCcw, Search, Settings, ShoppingBag,
  SlidersHorizontal, Trash2, Upload, Users, X,
} from 'lucide-react';
import { Link } from 'react-router-dom';
import { useAuth } from '../context/AuthContext.jsx';
import { getErrorMessage } from '../services/authService.js';
import { adminProductService } from '../services/adminProductService.js';
import './AdminProductsPage.css';

const emptyDraft = {
  name: '', slug: '', description: '', categoryId: '', audience: 'Women',
  price: '', salePrice: '', badge: '', isActive: true, imageUrl: '',
  imageFiles: [], colors: [{ name: 'Black', hexCode: '#171717' }],
  sizes: [{ name: 'S', stockQuantity: 0 }, { name: 'M', stockQuantity: 0 }],
};

function stockOf(product) {
  return product.sizes?.reduce((sum, size) => sum + Number(size.stockQuantity || 0), 0) || 0;
}

function normalizeAudience(value) {
  const audience = String(value || '').trim().toLowerCase();
  if (audience === 'men') return 'Men';
  if (audience === 'unisex') return 'Unisex';
  return 'Women';
}

function ProductEditor({ draft, categories, onChange, onClose, onSave }) {
  const fileInput = useRef(null);
  const [previews, setPreviews] = useState(draft.imageUrl ? [draft.imageUrl] : []);
  const update = (key, value) => onChange({ ...draft, [key]: value });
  const updateArray = (key, index, patch) => update(key, draft[key].map((item, i) => i === index ? { ...item, ...patch } : item));

  function addImages(event) {
    const files = [...event.target.files].slice(0, 5);
    const urls = files.map((file) => URL.createObjectURL(file));
    setPreviews((current) => [...current, ...urls].slice(0, 5));
    onChange({
      ...draft,
      imageUrl: draft.imageUrl || urls[0] || '',
      imageFiles: [...(draft.imageFiles || []), ...files].slice(0, 5),
    });
  }

  return (
    <div className="admin-drawer-backdrop" role="presentation" onMouseDown={(event) => event.target === event.currentTarget && onClose()}>
      <aside className="admin-product-drawer" role="dialog" aria-modal="true" aria-label="Product editor">
        <header className="admin-drawer-header">
          <div><span className="admin-eyebrow">{draft.id ? 'Edit product' : 'New product'}</span><h2>{draft.id ? draft.name : 'Create a product'}</h2></div>
          <button className="admin-icon-btn" onClick={onClose} aria-label="Close"><X size={20} /></button>
        </header>
        <div className="admin-drawer-body">
          <section className="admin-form-section">
            <div className="admin-section-heading"><span>01</span><div><h3>Product details</h3><p>The customer-facing product information.</p></div></div>
            <label className="admin-field admin-field-wide"><span>Product name</span><input value={draft.name} onChange={(e) => { const name = e.target.value; onChange({ ...draft, name, slug: draft.id ? draft.slug : name.toLowerCase().trim().replace(/[^a-z0-9]+/g, '-') }); }} placeholder="e.g. Relaxed linen shirt" /></label>
            <div className="admin-form-grid">
              <label className="admin-field"><span>Slug</span><input value={draft.slug} onChange={(e) => update('slug', e.target.value)} /></label>
              <label className="admin-field"><span>Badge</span><select value={draft.badge || ''} onChange={(e) => update('badge', e.target.value)}><option value="">No badge</option><option>New</option><option>Sale</option><option>Trending</option><option>Limited</option></select><ChevronDown size={15} /></label>
            </div>
            <label className="admin-field admin-field-wide"><span>Description</span><textarea rows="4" value={draft.description || ''} onChange={(e) => update('description', e.target.value)} placeholder="Describe the fit, fabric, and details…" /></label>
          </section>

          <section className="admin-form-section">
            <div className="admin-section-heading"><span>02</span><div><h3>Media</h3><p>Add up to five images. The first is used as the existing primary ImageUrl.</p></div></div>
            <button type="button" className="admin-upload-zone" onClick={() => fileInput.current?.click()}><Upload size={22} /><strong>Upload product images</strong><small>PNG, JPG or WEBP · up to 5 files</small></button>
            <input ref={fileInput} hidden type="file" accept="image/*" multiple onChange={addImages} />
            {previews.length > 0 && <div className="admin-image-strip">{previews.map((src, index) => <button type="button" className={index === 0 ? 'is-primary' : ''} key={src} onClick={() => { const nextPreviews = [src, ...previews.filter((item) => item !== src)]; const selectedFile = draft.imageFiles?.[index]; const nextFiles = selectedFile ? [selectedFile, ...draft.imageFiles.filter((_, fileIndex) => fileIndex !== index)] : draft.imageFiles; setPreviews(nextPreviews); onChange({ ...draft, imageUrl: src, imageFiles: nextFiles }); }}><img src={src} alt="" /><span>{index === 0 ? 'Primary' : 'Set primary'}</span></button>)}</div>}
          </section>

          <section className="admin-form-section">
            <div className="admin-section-heading"><span>03</span><div><h3>Organization & pricing</h3><p>Use the existing category, audience and price attributes.</p></div></div>
            <div className="admin-form-grid">
              <label className="admin-field"><span>Audience</span><select value={draft.audience || ''} onChange={(e) => { const audience = e.target.value; const selectedCategory = categories.find((item) => item.id === draft.categoryId); const categoryMatches = selectedCategory?.audience?.toLowerCase() === audience.toLowerCase(); onChange({ ...draft, audience, categoryId: categoryMatches ? draft.categoryId : '', categoryName: categoryMatches ? draft.categoryName : '' }); }}><option>Women</option><option>Men</option><option>Unisex</option></select><ChevronDown size={15} /></label>
              <label className="admin-field"><span>Category</span><select value={draft.categoryId || ''} onChange={(e) => { const category = categories.find((item) => item.id === e.target.value); const audience = category?.audience ? category.audience[0].toUpperCase() + category.audience.slice(1).toLowerCase() : draft.audience; onChange({ ...draft, categoryId: e.target.value, categoryName: category?.name || '', audience }); }}><option value="">{categories.length ? `Select ${draft.audience.toLowerCase()} category` : 'No categories loaded — restart API'}</option>{categories.filter((category) => category.audience?.toLowerCase() === draft.audience.toLowerCase()).map((category) => <option key={category.id} value={category.id}>{category.name}</option>)}</select><ChevronDown size={15} /></label>
              <label className="admin-field"><span>Price</span><i>$</i><input className="has-prefix" type="number" min="0" value={draft.price} onChange={(e) => update('price', e.target.value)} /></label>
              <label className="admin-field"><span>Sale price <em>preview only</em></span><i>$</i><input className="has-prefix" type="number" min="0" value={draft.salePrice || ''} onChange={(e) => update('salePrice', e.target.value)} placeholder="—" /></label>
            </div>
          </section>

          <section className="admin-form-section">
            <div className="admin-section-heading"><span>04</span><div><h3>Variants & inventory</h3><p>Stock is stored by size in the current ProductSize model.</p></div></div>
            <div className="admin-variant-title"><strong>Colors</strong><button type="button" onClick={() => update('colors', [...draft.colors, { name: '', hexCode: '#b7aa98' }])}><Plus size={14} /> Add color</button></div>
            <div className="admin-variant-list">{draft.colors.map((color, index) => <div className="admin-color-row" key={index}><input type="color" value={color.hexCode} onChange={(e) => updateArray('colors', index, { hexCode: e.target.value })} /><input aria-label="Color name" value={color.name} onChange={(e) => updateArray('colors', index, { name: e.target.value })} placeholder="Color name" /><input aria-label="Hex code" value={color.hexCode} onChange={(e) => updateArray('colors', index, { hexCode: e.target.value })} /><button type="button" onClick={() => update('colors', draft.colors.filter((_, i) => i !== index))}><Trash2 size={16} /></button></div>)}</div>
            <div className="admin-variant-title"><strong>Sizes & stock</strong><button type="button" onClick={() => update('sizes', [...draft.sizes, { name: '', stockQuantity: 0 }])}><Plus size={14} /> Add size</button></div>
            <div className="admin-stock-head"><span>Size</span><span>Available stock</span><span /></div>
            <div className="admin-variant-list">{draft.sizes.map((size, index) => <div className="admin-size-row" key={index}><input value={size.name} onChange={(e) => updateArray('sizes', index, { name: e.target.value })} placeholder="Size" /><input type="number" min="0" value={size.stockQuantity} onChange={(e) => updateArray('sizes', index, { stockQuantity: e.target.value })} /><button type="button" onClick={() => update('sizes', draft.sizes.filter((_, i) => i !== index))}><Trash2 size={16} /></button></div>)}</div>
          </section>

          <section className="admin-form-section admin-status-section">
            <div><h3>Product status</h3><p>Inactive products are not shown in the customer shop.</p></div>
            <button type="button" className={`admin-switch ${draft.isActive ? 'is-on' : ''}`} onClick={() => update('isActive', !draft.isActive)}><span /><b>{draft.isActive ? 'Active' : 'Inactive'}</b></button>
          </section>
        </div>
        <footer className="admin-drawer-footer"><button className="admin-secondary-btn" onClick={onClose}>Cancel</button><button className="admin-primary-btn" onClick={onSave} disabled={!draft.name || !draft.slug || !draft.price || !draft.imageUrl || !draft.categoryId}><Check size={17} /> Save product</button></footer>
      </aside>
    </div>
  );
}

export default function AdminProductsPage() {
  const { token, user } = useAuth();
  const [products, setProducts] = useState([]);
  const [categories, setCategories] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [query, setQuery] = useState('');
  const [status, setStatus] = useState('all');
  const [draft, setDraft] = useState(null);
  const [menu, setMenu] = useState(null);
  const [toast, setToast] = useState('');

  useEffect(() => {
    adminProductService.getCatalog(token).then((data) => {
      setCategories(data.categories || []);
      setProducts((data.products || []).map((product) => ({
        ...product,
        audience: normalizeAudience(product.audience),
      })));
    }).catch((requestError) => {
      setError(getErrorMessage(requestError, 'The product catalog could not be loaded.'));
    }).finally(() => setLoading(false));
  }, [token]);

  const filtered = useMemo(() => products.filter((product) => {
    const matchesQuery = `${product.name} ${product.categoryName} ${product.slug}`.toLowerCase().includes(query.toLowerCase());
    const matchesStatus = status === 'all' || (status === 'active' ? product.isActive && !product.archived : status === 'inactive' ? !product.isActive && !product.archived : product.archived);
    return matchesQuery && matchesStatus;
  }), [products, query, status]);

  const totalStock = products.reduce((sum, product) => sum + stockOf(product), 0);
  function notify(message) { setToast(message); window.setTimeout(() => setToast(''), 2400); }
  async function saveProduct() {
    try {
      setError('');
      let imageUrl = draft.imageUrl;
      if (draft.imageFiles?.[0]) {
        const upload = await adminProductService.uploadImage(token, draft.imageFiles[0]);
        imageUrl = upload.imageUrl;
      }

      const payload = {
        categoryId: draft.categoryId,
        name: draft.name,
        slug: draft.slug,
        description: draft.description || '',
        price: Number(draft.price),
        imageUrl,
        badge: draft.badge || null,
        isActive: Boolean(draft.isActive),
        colors: draft.colors.map(({ name, hexCode }) => ({ name, hexCode })),
        sizes: draft.sizes.map(({ name, stockQuantity }) => ({
          name,
          stockQuantity: Number(stockQuantity) || 0,
        })),
      };
      const saved = draft.id
        ? await adminProductService.update(token, draft.id, payload)
        : await adminProductService.create(token, payload);
      setProducts((current) => draft.id
        ? current.map((product) => product.id === saved.id ? saved : product)
        : [saved, ...current]);
      setDraft(null);
      notify(draft.id ? 'Product updated in database' : 'Product added to database');
    } catch (saveError) {
      setError(getErrorMessage(saveError, 'The product could not be saved.'));
    }
  }
  function archive(product) { setProducts((current) => current.map((item) => item.id === product.id ? { ...item, archived: true, isActive: false } : item)); setMenu(null); notify('Product archived'); }
  function restore(product) { setProducts((current) => current.map((item) => item.id === product.id ? { ...item, archived: false, isActive: true } : item)); setMenu(null); notify('Product restored'); }

  return (
    <div className="admin-shell">
      <aside className="admin-sidebar">
        <Link className="admin-brand" to="/shop"><span>V</span><div>VIRTUAL<strong>TRY-ON</strong></div></Link>
        <nav><small>WORKSPACE</small><Link to="/shop"><LayoutDashboard size={19} /> Dashboard</Link><a className="is-active"><ShoppingBag size={19} /> Products <b>{products.length}</b></a><a><Box size={19} /> Orders</a><a><Users size={19} /> Customers</a><small>MANAGE</small><a><SlidersHorizontal size={19} /> Categories</a><a><CircleDollarSign size={19} /> Promotions</a><a><Settings size={19} /> Settings</a></nav>
        <div className="admin-account"><div>{user?.fullName?.slice(0, 2).toUpperCase() || 'AD'}</div><span><strong>{user?.fullName || 'Admin User'}</strong><small>{user?.email || 'admin@atelier.com'}</small></span><MoreHorizontal size={18} /></div>
      </aside>

      <main className="admin-main">
        <header className="admin-topbar"><div><span>Catalog</span><b>/</b><strong>Products</strong></div><Link to="/shop"><Eye size={17} /> View storefront</Link></header>
        <div className="admin-content">
          <section className="admin-page-title"><div><span className="admin-eyebrow">PRODUCT MANAGEMENT</span><h1>Products</h1><p>Create, organize, and control what appears in your store.</p></div><button className="admin-primary-btn" onClick={() => setDraft({ ...emptyDraft })}><Plus size={18} /> Add product</button></section>
          {error && <div className="admin-notice">{error}</div>}
          <section className="admin-stats">
            <article><span><Package size={20} /></span><div><small>Total products</small><strong>{products.length}</strong><p><b>{products.filter((item) => item.isActive && !item.archived).length}</b> live in store</p></div></article>
            <article><span><Check size={20} /></span><div><small>Active products</small><strong>{products.filter((item) => item.isActive && !item.archived).length}</strong><p>{products.filter((item) => !item.isActive && !item.archived).length} inactive</p></div></article>
            <article><span><Box size={20} /></span><div><small>Total inventory</small><strong>{totalStock}</strong><p>units across all sizes</p></div></article>
            <article><span><Archive size={20} /></span><div><small>Low stock</small><strong>{products.filter((item) => stockOf(item) < 6).length}</strong><p>need attention</p></div></article>
          </section>

          <section className="admin-products-panel">
            <div className="admin-panel-toolbar">
              <div className="admin-status-tabs">{['all', 'active', 'inactive', 'archived'].map((item) => <button key={item} className={status === item ? 'is-active' : ''} onClick={() => setStatus(item)}>{item[0].toUpperCase() + item.slice(1)}{item === 'all' && <span>{products.length}</span>}</button>)}</div>
              <div className="admin-toolbar-actions"><label><Search size={17} /><input value={query} onChange={(e) => setQuery(e.target.value)} placeholder="Search products…" /></label><button><SlidersHorizontal size={17} /> Filter</button></div>
            </div>
            <div className="admin-table-wrap">
              <table className="admin-products-table">
                <thead><tr><th>Product</th><th>Category</th><th>Inventory</th><th>Price</th><th>Status</th><th /></tr></thead>
                <tbody>
                  {loading ? <tr><td colSpan="6" className="admin-empty">Loading catalog…</td></tr> : filtered.length === 0 ? <tr><td colSpan="6" className="admin-empty">No products match this view.</td></tr> : filtered.map((product) => {
                    const inventory = stockOf(product);
                    return <tr key={product.id}>
                      <td><div className="admin-product-cell"><div className="admin-product-thumb">{product.imageUrl ? <img src={product.imageUrl} alt="" /> : <ImagePlus size={20} />}</div><span><strong>{product.name}</strong><small>/{product.slug}</small><i>{product.colors?.slice(0, 4).map((color) => <b key={color.name} style={{ background: color.hexCode }} title={color.name} />)}{product.colors?.length ? <em>{product.colors.length} colors</em> : null}</i></span></div></td>
                      <td><strong>{product.categoryName || 'Uncategorized'}</strong><small>{product.audience}</small></td>
                      <td><strong>{inventory} in stock</strong><small className={inventory < 6 ? 'is-low' : ''}>{inventory < 6 ? 'Low inventory' : `${product.sizes?.length || 0} sizes`}</small></td>
                      <td><strong>${Number(product.price).toFixed(2)}</strong>{product.badge && <small>{product.badge}</small>}</td>
                      <td><span className={`admin-status ${product.archived ? 'is-archived' : product.isActive ? 'is-active' : 'is-hidden'}`}><i />{product.archived ? 'Archived' : product.isActive ? 'Active' : 'Inactive'}</span></td>
                      <td className="admin-menu-cell"><button onClick={() => setMenu(menu === product.id ? null : product.id)}><MoreHorizontal size={19} /></button>{menu === product.id && <div className="admin-row-menu"><button onClick={() => { setDraft({ ...product, audience: normalizeAudience(product.audience), imageFiles: [], salePrice: product.salePrice || '' }); setMenu(null); }}><Pencil size={15} /> Edit product</button><Link to={`/shop/products/${product.id}`}><Eye size={15} /> Preview page</Link>{product.archived ? <button onClick={() => restore(product)}><RefreshCcw size={15} /> Restore</button> : <button className="is-danger" onClick={() => archive(product)}><Archive size={15} /> Archive</button>}</div>}</td>
                    </tr>;
                  })}
                </tbody>
              </table>
            </div>
            <footer className="admin-table-footer"><span>Showing <strong>{filtered.length}</strong> of {products.length} products</span><div><button disabled><ArrowLeft size={15} /></button><button className="is-active">1</button><button disabled>2</button><button disabled><ArrowLeft size={15} /></button></div></footer>
          </section>
        </div>
      </main>
      {draft && <ProductEditor draft={draft} categories={categories} onChange={setDraft} onClose={() => setDraft(null)} onSave={saveProduct} />}
      {toast && <div className="admin-toast"><Check size={17} /> {toast}</div>}
    </div>
  );
}
