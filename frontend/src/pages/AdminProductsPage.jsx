import { useEffect, useMemo, useRef, useState } from 'react';
import {
  Archive, Box, Check, ChevronDown, ChevronLeft, ChevronRight,
  CircleDollarSign, Download, Eye, ImagePlus, LayoutDashboard, MoreHorizontal,
  Package, Pencil, Plus, RefreshCcw, Search, ShoppingBag,
  Trash2, Upload, Users, X,
} from 'lucide-react';
import { Link } from 'react-router-dom';
import { useAuth } from '../context/AuthContext.jsx';
import AdminSidebar from '../components/admin/AdminSidebar.jsx';
import { getErrorMessage } from '../services/authService.js';
import { adminProductService } from '../services/adminProductService.js';
import './AdminProductsPage.css';

const emptyDraft = {
  name: '', slug: '', description: '', categoryId: '', audience: 'Women',
  price: '', salePrice: '', badge: '', isActive: true,
  imageUrl: '', originalImageUrl: '',
  aiMaskFile: null, aiMaskUrl: '', aiMaskImageUrl: '', maskApproved: false,
  positivePoints: [], negativePoints: [], maskScore: 0,
  imageFiles: [], colors: [
  {
    name: 'Black',
    hexCode: '#171717',
    imageUrl: null,
  },
],
  sizes: [{ name: 'S', stockQuantity: 0 }, { name: 'M', stockQuantity: 0 }],
};

const PRODUCTS_PER_PAGE = 8;

function stockOf(product) {
  return product.sizes?.reduce((sum, size) => sum + Number(size.stockQuantity || 0), 0) || 0;
}

function normalizeAudience(value) {
  const audience = String(value || '').trim().toLowerCase();
  if (audience === 'men') return 'Men';
  if (audience === 'unisex') return 'Unisex';
  return 'Women';
}

function getAiItemType(category) {
  const slug = String(category?.slug || '').toLowerCase();

  if (slug.includes('dress')) return 'dress';
  if (slug.includes('skirt')) return 'skirt';
  if (slug.includes('shoe')) return 'shoes';
  if (slug.includes('accessor')) return 'bag';
  if (
    slug.includes('trouser') ||
    slug.includes('bottom') ||
    slug.includes('pant')
  ) {
    return 'pants';
  }

  return 'upper-clothes';
}

async function getImageFile(imageUrl) {
  const requestUrl = new URL(imageUrl);
  requestUrl.searchParams.set('_mediaVersion', Date.now().toString());
  const response = await fetch(requestUrl.toString(), { cache: 'no-store' });

  if (!response.ok) {
    throw new Error('The original product image could not be loaded.');
  }

  const blob = await response.blob();
  return new File([blob], 'original-product-image', {
    type: blob.type || 'image/png',
  });
}

function safeFilePart(value) {
  return String(value || 'color')
    .trim()
    .toLowerCase()
    .replace(/[^a-z0-9]+/g, '-')
    .replace(/^-|-$/g, '');
}

function colorNeedsAiImage(color) {
  return Boolean(
    color?.name?.trim()
    && color?.hexCode?.trim()
    && !color?.imageUrl,
  );
}

function ProductEditor({ draft, categories, onChange, onClose, onSave, saving }) {
  const fileInput = useRef(null);
  const sourceImageUrl = draft.originalImageUrl || draft.imageUrl;
  const [previews, setPreviews] = useState(sourceImageUrl ? [sourceImageUrl] : []);
  const [segmenting, setSegmenting] = useState(false);
  const [maskError, setMaskError] = useState('');
  const requiresAiMask = draft.colors?.some(colorNeedsAiImage) || false;
  const hasReusableMask = Boolean(draft.aiMaskImageUrl) && !draft.aiMaskFile;
  const hasPendingMask = Boolean(draft.aiMaskFile && !draft.maskApproved);
  const update = (key, value) => onChange({ ...draft, [key]: value });
  const updateArray = (key, index, patch) => update(key, draft[key].map((item, i) => i === index ? { ...item, ...patch } : item));

  function addImages(event) {
    const files = [...event.target.files].slice(0, 5);
    const urls = files.map((file) => URL.createObjectURL(file));
    setPreviews((current) => [...current, ...urls].slice(0, 5));
    onChange({
      ...draft,
      originalImageUrl: urls[0] || sourceImageUrl || '',
      imageUrl: urls[0] || sourceImageUrl || '',
      imageFiles: [...(draft.imageFiles || []), ...files].slice(0, 5),
      aiMaskFile: null,
      aiMaskUrl: '',
      aiMaskImageUrl: '',
      maskApproved: false,
      positivePoints: [],
      negativePoints: [],
      maskScore: 0,
    });
  }

  function removeImage(index) {
    const removedUrl = previews[index];
    if (removedUrl?.startsWith('blob:')) {
      URL.revokeObjectURL(removedUrl);
    }

    const nextPreviews = previews.filter((_, itemIndex) => itemIndex !== index);
    const nextFiles = (draft.imageFiles || []).filter(
      (_, itemIndex) => itemIndex !== index,
    );
    const nextOriginalUrl = nextPreviews[0] || '';

    setPreviews(nextPreviews);
    onChange({
      ...draft,
      originalImageUrl: nextOriginalUrl,
      imageUrl: nextOriginalUrl,
      imageFiles: nextFiles,
      aiMaskFile: null,
      aiMaskUrl: '',
      aiMaskImageUrl: '',
      maskApproved: false,
      positivePoints: [],
      negativePoints: [],
      maskScore: 0,
    });
  }

  function addMaskPoint(event) {
    event.preventDefault();

    if (draft.aiMaskUrl?.startsWith('blob:')) {
      URL.revokeObjectURL(draft.aiMaskUrl);
    }

    const rectangle =
      event.currentTarget.getBoundingClientRect();

    const point = [
      (event.clientX - rectangle.left) /
        rectangle.width,
      (event.clientY - rectangle.top) /
        rectangle.height,
    ];

    const isNegative =
      event.shiftKey || event.button === 2;

    onChange({
      ...draft,
      aiMaskFile: null,
      aiMaskUrl: '',
      aiMaskImageUrl: '',
      positivePoints: isNegative
        ? draft.positivePoints || []
        : [...(draft.positivePoints || []), point],
      negativePoints: isNegative
        ? [...(draft.negativePoints || []), point]
        : draft.negativePoints || [],
      maskApproved: false,
      maskScore: 0,
    });
  }

  async function generateMask() {
    try {
      setMaskError('');
      setSegmenting(true);
      const sourceFile = draft.imageFiles?.[0]
        || await getImageFile(draft.originalImageUrl || draft.imageUrl);
      const result = await adminProductService.segmentImage(
        sourceFile,
        draft.positivePoints || [],
        draft.negativePoints || [],
      );
      if (draft.aiMaskUrl?.startsWith('blob:')) {
        URL.revokeObjectURL(draft.aiMaskUrl);
      }
      const maskUrl = URL.createObjectURL(result.blob);
      const maskFile = new File([result.blob], 'approved-product-mask.png', {
        type: 'image/png',
      });
      onChange({
        ...draft,
        aiMaskFile: maskFile,
        aiMaskUrl: maskUrl,
        aiMaskImageUrl: '',
        colors: (draft.colors || []).map((color) => ({
          ...color,
          imageUrl: null,
        })),
        maskApproved: false,
        maskScore: result.score,
      });
    } catch (error) {
      setMaskError(error.message || 'The product mask could not be generated.');
    } finally {
      setSegmenting(false);
    }
  }

  function resetMask() {
    if (draft.aiMaskUrl?.startsWith('blob:')) {
      URL.revokeObjectURL(draft.aiMaskUrl);
    }
    onChange({
      ...draft,
      aiMaskFile: null,
      aiMaskUrl: '',
      aiMaskImageUrl: '',
      maskApproved: false,
      positivePoints: [],
      negativePoints: [],
      maskScore: 0,
    });
    setMaskError('');
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
            <div className="admin-section-heading"><span>AI</span><div><h3>Approve product mask</h3><p>Click the product. Shift-click or right-click anything that must not change.</p></div></div>
            {sourceImageUrl ? (
              <>
                <div style={{ position: 'relative', width: '100%', overflow: 'hidden', borderRadius: 12, background: '#eee' }}>
                  <img
                    src={sourceImageUrl}
                    alt="Select the product"
                    onClick={addMaskPoint}
                    onContextMenu={addMaskPoint}
                    style={{ display: 'block', width: '100%', height: 'auto', cursor: 'crosshair' }}
                  />
                  {draft.aiMaskUrl && (
                    <img
                      src={draft.aiMaskUrl}
                      alt="MobileSAM mask"
                      style={{ position: 'absolute', inset: 0, width: '100%', height: '100%', opacity: 0.48, pointerEvents: 'none' }}
                    />
                  )}
                  {(draft.positivePoints || []).map(([x, y], index) => (
                    <span key={`positive-${index}`} style={{ position: 'absolute', left: `${x * 100}%`, top: `${y * 100}%`, width: 14, height: 14, borderRadius: '50%', background: '#20a34a', border: '2px solid white', transform: 'translate(-50%, -50%)', pointerEvents: 'none' }} />
                  ))}
                  {(draft.negativePoints || []).map(([x, y], index) => (
                    <span key={`negative-${index}`} style={{ position: 'absolute', left: `${x * 100}%`, top: `${y * 100}%`, width: 14, height: 14, borderRadius: '50%', background: '#dc2626', border: '2px solid white', transform: 'translate(-50%, -50%)', pointerEvents: 'none' }} />
                  ))}
                </div>
                <p style={{ marginTop: 8, fontSize: 12 }}>Green = include product · Red = exclude background, skin, logos or nearby objects.</p>
                {maskError && <p style={{ color: '#b42318', fontSize: 13 }}>{maskError}</p>}
                {draft.maskScore > 0 && <p style={{ fontSize: 13 }}>AI confidence: {(draft.maskScore * 100).toFixed(1)}%</p>}
                <div style={{ display: 'flex', flexWrap: 'wrap', gap: 8, marginTop: 10 }}>
                  <button type="button" className="admin-secondary-btn" onClick={generateMask} disabled={segmenting || !(draft.positivePoints || []).length}>
                    <RefreshCcw size={15} /> {segmenting ? 'Detecting…' : draft.aiMaskUrl ? 'Update mask' : 'Generate mask'}
                  </button>
                  <button type="button" className="admin-secondary-btn" onClick={resetMask} disabled={segmenting}>Reset points</button>
                  <button
                    type="button"
                    className="admin-primary-btn"
                    disabled={!draft.aiMaskFile || segmenting}
                    onClick={() => update('maskApproved', true)}
                  >
                    <Check size={15} /> {draft.maskApproved ? 'Mask approved' : 'Approve this mask'}
                  </button>
                </div>
              </>
            ) : <p>Upload the primary product image first.</p>}
            {hasReusableMask && !draft.aiMaskFile && (
              <p style={{ marginTop: 8, color: '#287a45', fontSize: 13 }}>
                The previously approved mask will be reused automatically.
              </p>
            )}
          </section>

          <section className="admin-form-section">
            <div className="admin-section-heading"><span>02</span><div><h3>Media</h3><p>Add up to five images. The first is used as the existing primary ImageUrl.</p></div></div>
            <button type="button" className="admin-upload-zone" onClick={() => fileInput.current?.click()}><Upload size={22} /><strong>Upload product images</strong><small>PNG, JPG or WEBP · up to 5 files</small></button>
            <input ref={fileInput} hidden type="file" accept="image/*" multiple onChange={addImages} />
            {previews.length > 0 && (
              <div className="admin-image-strip">
                {previews.map((src, index) => (
                  <div key={src} style={{ position: 'relative' }}>
                    <button
                      type="button"
                      className={index === 0 ? 'is-primary' : ''}
                      onClick={() => {
                        const nextPreviews = [
                          src,
                          ...previews.filter((item) => item !== src),
                        ];
                        const selectedFile = draft.imageFiles?.[index];
                        const nextFiles = selectedFile
                          ? [
                              selectedFile,
                              ...draft.imageFiles.filter(
                                (_, fileIndex) => fileIndex !== index,
                              ),
                            ]
                          : draft.imageFiles;
                        setPreviews(nextPreviews);
                        onChange({
                          ...draft,
                          originalImageUrl: src,
                          imageUrl: src,
                          imageFiles: nextFiles,
                          aiMaskFile: null,
                          aiMaskUrl: '',
                          aiMaskImageUrl: '',
                          maskApproved: false,
                          positivePoints: [],
                          negativePoints: [],
                          maskScore: 0,
                        });
                      }}
                    >
                      <img src={src} alt="" />
                      <span>{index === 0 ? 'Original' : 'Set original'}</span>
                    </button>
                    <button
                      type="button"
                      aria-label="Remove image"
                      title="Remove image"
                      onClick={() => removeImage(index)}
                      style={{
                        position: 'absolute',
                        top: 4,
                        right: 4,
                        width: 24,
                        height: 24,
                        borderRadius: '50%',
                        border: '1px solid #ddd',
                        background: '#fff',
                        display: 'grid',
                        placeItems: 'center',
                        cursor: 'pointer',
                        zIndex: 2,
                      }}
                    >
                      <X size={14} />
                    </button>
                  </div>
                ))}
              </div>
            )}
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
            <div className="admin-variant-title"><strong>Colors</strong><button type="button" onClick={() => update('colors', [...draft.colors, {
  name: '',
  hexCode: '#b7aa98',
  imageUrl: null,
}])}><Plus size={14} /> Add color</button></div>
            <div className="admin-variant-list">{draft.colors.map((color, index) => <div className="admin-color-row" key={index}><input type="color" value={color.hexCode} onChange={(e) => updateArray('colors', index, { hexCode: e.target.value, imageUrl: null })} /><input aria-label="Color name" value={color.name} onChange={(e) => updateArray('colors', index, { name: e.target.value })} placeholder="Color name" /><input aria-label="Hex code" value={color.hexCode} onChange={(e) => updateArray('colors', index, { hexCode: e.target.value, imageUrl: null })} /><button type="button" onClick={() => update('colors', draft.colors.filter((_, i) => i !== index))}><Trash2 size={16} /></button></div>)}</div>
            <div className="admin-variant-title"><strong>Sizes & stock</strong><button type="button" onClick={() => update('sizes', [...draft.sizes, { name: '', stockQuantity: 0 }])}><Plus size={14} /> Add size</button></div>
            <div className="admin-stock-head"><span>Size</span><span>Available stock</span><span /></div>
            <div className="admin-variant-list">{draft.sizes.map((size, index) => <div className="admin-size-row" key={index}><input value={size.name} onChange={(e) => updateArray('sizes', index, { name: e.target.value })} placeholder="Size" /><input type="number" min="0" value={size.stockQuantity} onChange={(e) => updateArray('sizes', index, { stockQuantity: e.target.value })} /><button type="button" onClick={() => update('sizes', draft.sizes.filter((_, i) => i !== index))}><Trash2 size={16} /></button></div>)}</div>
          </section>

          <section className="admin-form-section admin-status-section">
            <div><h3>Product status</h3><p>Inactive products are not shown in the customer shop.</p></div>
            <button type="button" className={`admin-switch ${draft.isActive ? 'is-on' : ''}`} onClick={() => update('isActive', !draft.isActive)}><span /><b>{draft.isActive ? 'Active' : 'Inactive'}</b></button>
          </section>
        </div>
        <footer className="admin-drawer-footer"><button className="admin-secondary-btn" onClick={onClose} disabled={saving}>Cancel</button><button className="admin-primary-btn" onClick={onSave} disabled={saving || !draft.name || !draft.slug || !draft.price || !sourceImageUrl || !draft.categoryId || hasPendingMask || (requiresAiMask && !hasReusableMask && !draft.maskApproved)}><Check size={17} /> {saving ? (requiresAiMask ? 'Generating approved colors…' : 'Saving product…') : hasPendingMask || (requiresAiMask && !hasReusableMask && !draft.maskApproved) ? 'Approve mask before saving' : 'Save product'}</button></footer>
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
  const [categoryFilter, setCategoryFilter] = useState('all');
  const [audienceFilter, setAudienceFilter] = useState('all');
  const [sortBy, setSortBy] = useState('default');
  const [page, setPage] = useState(1);
  const [draft, setDraft] = useState(null);
  const [menu, setMenu] = useState(null);
  const [toast, setToast] = useState('');
  const [saving, setSaving] = useState(false);

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

  const filtered = useMemo(() => {
    const normalizedQuery = query.trim().toLowerCase();
    const results = products.filter((product) => {
      const inventory = stockOf(product);
      const matchesQuery = [product.name, product.categoryName, product.slug, product.audience, product.badge]
        .filter(Boolean).join(' ').toLowerCase().includes(normalizedQuery);
      const matchesStatus = status === 'all'
        || (status === 'active' && product.isActive && !product.archived)
        || (status === 'inactive' && !product.isActive && !product.archived)
        || (status === 'low-stock' && inventory > 0 && inventory < 6)
        || (status === 'out-of-stock' && inventory === 0);
      return matchesQuery && matchesStatus
        && (categoryFilter === 'all' || product.categoryId === categoryFilter)
        && (audienceFilter === 'all' || normalizeAudience(product.audience) === audienceFilter);
    });
    return [...results].sort((first, second) => {
      if (sortBy === 'name-asc') return first.name.localeCompare(second.name);
      if (sortBy === 'name-desc') return second.name.localeCompare(first.name);
      if (sortBy === 'price-low') return Number(first.price) - Number(second.price);
      if (sortBy === 'price-high') return Number(second.price) - Number(first.price);
      if (sortBy === 'stock-low') return stockOf(first) - stockOf(second);
      if (sortBy === 'stock-high') return stockOf(second) - stockOf(first);
      return 0;
    });
  }, [products, query, status, categoryFilter, audienceFilter, sortBy]);

  const totalPages = Math.max(1, Math.ceil(filtered.length / PRODUCTS_PER_PAGE));
  const visibleProducts = filtered.slice((page - 1) * PRODUCTS_PER_PAGE, page * PRODUCTS_PER_PAGE);

  useEffect(() => { setPage(1); }, [query, status, categoryFilter, audienceFilter, sortBy]);
  useEffect(() => { if (page > totalPages) setPage(totalPages); }, [page, totalPages]);

  const totalStock = products.reduce((sum, product) => sum + stockOf(product), 0);
  function notify(message) { setToast(message); window.setTimeout(() => setToast(''), 2400); }
  async function saveProduct() {
    try {
      setError('');
      const shouldRegenerateColors = Boolean(
        draft.maskApproved && draft.aiMaskFile,
      );
      const colorsNeedingAiImage = (draft.colors || []).filter(
        (color) => color?.name?.trim() && color?.hexCode?.trim()
          && (shouldRegenerateColors || colorNeedsAiImage(color)),
      );

      setSaving(true);

      let originalImageUrl = draft.originalImageUrl || draft.imageUrl;

      const originalFile = draft.imageFiles?.[0] || null;

      if (originalFile) {
        const upload = await adminProductService.uploadImage(
          token,
          originalFile,
        );
        originalImageUrl = upload.imageUrl;
      }

      let aiMaskImageUrl = draft.aiMaskImageUrl || '';
      let approvedMaskFile = draft.maskApproved ? draft.aiMaskFile : null;

      if (draft.maskApproved && draft.aiMaskFile) {
        const maskUpload = await adminProductService.uploadImage(
          token,
          draft.aiMaskFile,
        );
        aiMaskImageUrl = maskUpload.imageUrl;
      }

      if (
        colorsNeedingAiImage.length > 0
        && !approvedMaskFile
        && aiMaskImageUrl
      ) {
        approvedMaskFile = await getImageFile(aiMaskImageUrl);
      }

      if (colorsNeedingAiImage.length > 0 && !approvedMaskFile) {
        throw new Error(
          'Generate and approve a product mask before creating new color images.',
        );
      }

      let sourceFile = originalFile;

      if (!sourceFile && colorsNeedingAiImage.length > 0) {
        sourceFile = await getImageFile(originalImageUrl);
      }

      const selectedCategory = categories.find(
        (category) => category.id === draft.categoryId,
      );
      const itemType = getAiItemType(selectedCategory);
      const generatedColors = [];

      for (const color of draft.colors) {
        if (!color.name?.trim() || !color.hexCode?.trim()) {
          continue;
        }

        if (color.imageUrl && !shouldRegenerateColors) {
          generatedColors.push({
            name: color.name.trim(),
            hexCode: color.hexCode.trim(),
            imageUrl: color.imageUrl,
          });
          continue;
        }

        const recoloredBlob = await adminProductService.recolorImage(
          sourceFile,
          approvedMaskFile,
          color.hexCode,
          itemType,
        );

        const generatedFileName = [
          safeFilePart(draft.slug),
          safeFilePart(color.name),
        ].join('-');

        const generatedFile = new File(
          [recoloredBlob],
          `${generatedFileName}.png`,
          { type: 'image/png' },
        );

        const generatedUpload = await adminProductService.uploadImage(
          token,
          generatedFile,
        );

        generatedColors.push({
          name: color.name.trim(),
          hexCode: color.hexCode.trim(),
          imageUrl: generatedUpload.imageUrl,
        });
      }

      const payload = {
        categoryId: draft.categoryId,
        name: draft.name,
        slug: draft.slug,
        description: draft.description || '',
        price: Number(draft.price),
        imageUrl: originalImageUrl,
        originalImageUrl,
        aiMaskImageUrl: aiMaskImageUrl || null,
        badge: draft.badge || null,
        isActive: Boolean(draft.isActive),
        colors: generatedColors,
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
      notify(draft.id ? 'Product and color images updated' : 'Product and AI color images added');
    } catch (saveError) {
      setError(getErrorMessage(saveError, 'The product could not be saved.'));
    } finally {
      setSaving(false);
    }
  }
  async function setProductActive(product, isActive) {
    try {
      setError(''); setMenu(null);
      const saved = await adminProductService.update(token, product.id, {
        categoryId: product.categoryId, name: product.name, slug: product.slug,
        description: product.description || '', price: Number(product.price),
        imageUrl: product.imageUrl, originalImageUrl: product.originalImageUrl || product.imageUrl,
        aiMaskImageUrl: product.aiMaskImageUrl || null, badge: product.badge || null, isActive,
        colors: (product.colors || []).map(({ name, hexCode, imageUrl }) => ({ name, hexCode, imageUrl: imageUrl || null })),
        sizes: (product.sizes || []).map(({ name, stockQuantity }) => ({ name, stockQuantity: Number(stockQuantity) || 0 })),
      });
      setProducts((current) => current.map((item) => item.id === product.id ? { ...saved, archived: false } : item));
      notify(isActive ? 'Product activated' : 'Product hidden from the store');
    } catch (requestError) { setError(getErrorMessage(requestError, 'The product status could not be updated.')); }
  }
  async function deleteProduct(product) {
    if (!window.confirm(`Permanently delete "${product.name}"? This action cannot be undone.`)) return;
    try {
      setError('');
      setMenu(null);
      await adminProductService.delete(token, product.id);
      setProducts((current) => current.filter((item) => item.id !== product.id));
      notify('Product permanently deleted');
    } catch (deleteError) {
      setError(getErrorMessage(deleteError, 'The product could not be deleted.'));
    }
  }

  function exportProducts() {
    if (!filtered.length) { setError('There are no products to export.'); return; }
    const escapeCsv = (value) => `"${String(value ?? '').replace(/"/g, '""')}"`;
    const rows = filtered.map((product) => [
      product.name, product.slug, product.categoryName, product.audience,
      product.price, product.salePrice || '', stockOf(product),
      product.colors?.map((color) => color.name).join(' | ') || '',
      product.sizes?.map((size) => `${size.name}: ${size.stockQuantity}`).join(' | ') || '',
      product.archived ? 'Archived' : product.isActive ? 'Active' : 'Inactive',
    ]);
    const csv = [['Name', 'Slug', 'Category', 'Audience', 'Price', 'Sale Price', 'Stock', 'Colors', 'Sizes', 'Status'], ...rows]
      .map((row) => row.map(escapeCsv).join(',')).join('\n');
    const url = URL.createObjectURL(new Blob([`\uFEFF${csv}`], { type: 'text/csv;charset=utf-8' }));
    const link = document.createElement('a');
    link.href = url;
    link.download = `products-${new Date().toISOString().slice(0, 10)}.csv`;
    document.body.appendChild(link); link.click(); link.remove(); URL.revokeObjectURL(url);
  }

  return (
    <div className="admin-shell">
      <AdminSidebar user={user} counts={{ '/admin/products': products.length }} />

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
              <div className="admin-status-tabs">{[['all','All'],['active','Active'],['inactive','Inactive'],['low-stock','Low stock'],['out-of-stock','Out of stock']].map(([value,label]) => <button type="button" key={value} className={status === value ? 'is-active' : ''} onClick={() => setStatus(value)}>{label}{value === 'all' && <span>{products.length}</span>}</button>)}</div>
              <div className="admin-toolbar-actions product-toolbar-actions">
                <label className="product-search"><Search size={17}/><input value={query} onChange={(event)=>setQuery(event.target.value)} placeholder="Search products…"/></label>
                <label className="product-filter-select"><select value={audienceFilter} onChange={(event)=>setAudienceFilter(event.target.value)}><option value="all">All audiences</option><option>Women</option><option>Men</option><option>Unisex</option></select><ChevronDown size={14}/></label>
                <label className="product-filter-select"><select value={categoryFilter} onChange={(event)=>setCategoryFilter(event.target.value)}><option value="all">All categories</option>{categories.map((category)=><option key={category.id} value={category.id}>{category.name}</option>)}</select><ChevronDown size={14}/></label>
                <label className="product-filter-select"><select value={sortBy} onChange={(event)=>setSortBy(event.target.value)}><option value="default">Default order</option><option value="name-asc">Name: A–Z</option><option value="name-desc">Name: Z–A</option><option value="price-low">Price: low to high</option><option value="price-high">Price: high to low</option><option value="stock-low">Stock: low to high</option><option value="stock-high">Stock: high to low</option></select><ChevronDown size={14}/></label>
                <button type="button" className="product-export-button" onClick={exportProducts} disabled={!filtered.length}><Download size={16}/> Export</button>
              </div>
            </div>
            <div className="admin-table-wrap">
              <table className="admin-products-table">
                <thead><tr><th>Product</th><th>Category</th><th>Inventory</th><th>Price</th><th>Sale price</th><th>Status</th><th /></tr></thead>
                <tbody>
                  {loading ? <tr><td colSpan="7" className="admin-empty">Loading catalog…</td></tr> : filtered.length === 0 ? <tr><td colSpan="7" className="admin-empty">No products match this view.</td></tr> : visibleProducts.map((product) => {
                    const inventory = stockOf(product);
                    return <tr key={product.id}>
                      <td><div className="admin-product-cell"><div className="admin-product-thumb">{product.imageUrl ? <img src={product.imageUrl} alt="" /> : <ImagePlus size={20} />}</div><span><strong>{product.name}</strong><small>/{product.slug}</small><i>{product.colors?.slice(0, 4).map((color) => <b key={color.name} style={{ background: color.hexCode }} title={color.name} />)}{product.colors?.length ? <em>{product.colors.length} colors</em> : null}</i></span></div></td>
                      <td><strong>{product.categoryName || 'Uncategorized'}</strong><small>{product.audience}</small></td>
                      <td><strong>{inventory} in stock</strong><small className={inventory < 6 ? 'is-low' : ''}>{inventory < 6 ? 'Low inventory' : `${product.sizes?.length || 0} sizes`}</small></td>
                      <td><strong>${Number(product.price).toFixed(2)}</strong>{product.badge && <small>{product.badge}</small>}</td>
                      <td>{product.salePrice != null ? <><strong className="admin-sale-price">${Number(product.salePrice).toFixed(2)}</strong><small>{Number(product.discountPercentage).toFixed(0)}% off</small></> : <span className="admin-no-sale">—</span>}</td>
                      <td><span className={`admin-status ${product.archived ? 'is-archived' : product.isActive ? 'is-active' : 'is-hidden'}`}><i />{product.archived ? 'Archived' : product.isActive ? 'Active' : 'Inactive'}</span></td>
                      <td className="admin-menu-cell"><button onClick={() => setMenu(menu === product.id ? null : product.id)}><MoreHorizontal size={19} /></button>{menu === product.id && <div className="admin-row-menu"><button onClick={() => { setDraft({ ...product, originalImageUrl: product.originalImageUrl || product.imageUrl, audience: normalizeAudience(product.audience), imageFiles: [], salePrice: product.salePrice || '' }); setMenu(null); }}><Pencil size={15} /> Edit product</button><Link to={`/shop/products/${product.id}`}><Eye size={15} /> Preview page</Link><button onClick={() => setProductActive(product, !product.isActive)}>{product.isActive ? <Archive size={15}/> : <RefreshCcw size={15}/>} {product.isActive ? 'Hide from store' : 'Activate product'}</button><button className="is-danger" onClick={() => deleteProduct(product)}><Trash2 size={15} /> Delete permanently</button></div>}</td>
                    </tr>;
                  })}
                </tbody>
              </table>
            </div>
            <footer className="admin-table-footer professional-product-footer"><span>Showing <strong>{visibleProducts.length}</strong> of {filtered.length} filtered products</span><div><button type="button" disabled={page===1} onClick={()=>setPage((current)=>Math.max(1,current-1))}><ChevronLeft size={15}/></button><span className="product-page-number">Page {page} of {totalPages}</span><button type="button" disabled={page===totalPages} onClick={()=>setPage((current)=>Math.min(totalPages,current+1))}><ChevronRight size={15}/></button></div></footer>
          </section>
        </div>
      </main>
      {draft && <ProductEditor draft={draft} categories={categories} onChange={setDraft} onClose={() => setDraft(null)} onSave={saveProduct} saving={saving} />}
      {toast && <div className="admin-toast"><Check size={17} /> {toast}</div>}
    </div>
  );
}





