import { useEffect, useMemo, useState } from 'react';
import { ArrowLeft, Check, LayoutDashboard, Mail, PackageCheck, Percent, RefreshCw, Search, Send, Tag, Users, X } from 'lucide-react';
import { Link } from 'react-router-dom';
import AdminSidebar from '../components/admin/AdminSidebar.jsx';
import { useAuth } from '../context/AuthContext.jsx';
import { adminProductService } from '../services/adminProductService.js';
import { promotionService } from '../services/promotionService.js';
import { getErrorMessage } from '../services/authService.js';
import './AdminProductsPage.css';
import './AdminPromotionsPage.css';

function localInput(date) {
  const shifted = new Date(date.getTime() - date.getTimezoneOffset() * 60000);
  return shifted.toISOString().slice(0, 16);
}

export default function AdminPromotionsPage() {
  const { token, user } = useAuth();
  const [promotions, setPromotions] = useState([]);
  const [products, setProducts] = useState([]);
  const [query, setQuery] = useState('');
  const [error, setError] = useState('');
  const [success, setSuccess] = useState('');
  const [saving, setSaving] = useState(false);
  const [loading, setLoading] = useState(true);
  const [campaignQuery, setCampaignQuery] = useState('');
  const [campaignStatus, setCampaignStatus] = useState('All');
  const [campaignBusy, setCampaignBusy] = useState('');
  const [form, setForm] = useState({
    name: '', discountPercentage: 20,
    startsAt: localInput(new Date()),
    endsAt: localInput(new Date(Date.now() + 7 * 86400000)),
    productIds: [], sendEmail: true,
  });

  async function load() {
    try {
      setLoading(true); setError('');
      const [campaigns, catalog] = await Promise.all([
        promotionService.getAll(token), adminProductService.getCatalog(token),
      ]);
      setPromotions(campaigns); setProducts(catalog.products || []);
    } catch (loadError) { setError(getErrorMessage(loadError)); }
    finally { setLoading(false); }
  }
  useEffect(() => { load(); }, [token]);

  const visibleProducts = useMemo(() => products.filter((product) =>
    `${product.name} ${product.categoryName} ${product.audience}`.toLowerCase().includes(query.toLowerCase())), [products, query]);

  const visiblePromotions = useMemo(() => promotions.filter((promotion) => {
    const now = new Date(); const starts = new Date(promotion.startsAt); const ends = new Date(promotion.endsAt);
    const state = !promotion.isActive ? 'Inactive' : starts > now ? 'Scheduled' : ends < now ? 'Expired' : 'Active';
    return (campaignStatus === 'All' || state === campaignStatus)
      && promotion.name.toLowerCase().includes(campaignQuery.trim().toLowerCase());
  }), [promotions, campaignQuery, campaignStatus]);

  async function resendCampaign(promotion) {
    try { setCampaignBusy(promotion.id); setError('');
      const updated = await promotionService.send(token, promotion.id);
      setPromotions((all) => all.map((item) => item.id === updated.id ? updated : item));
      setSuccess(`Campaign delivery completed: ${updated.emailSuccessCount} sent, ${updated.emailFailureCount} failed.`);
    } catch (sendError) { setError(getErrorMessage(sendError, 'The campaign could not be sent.')); }
    finally { setCampaignBusy(''); }
  }


  async function reactivateCampaign(promotion) {
    try {
      setCampaignBusy(promotion.id);
      setError('');
      setSuccess('');

      const updated = await promotionService.reactivate(
        token,
        promotion.id
      );

      setPromotions((all) =>
        all.map((item) =>
          item.id === updated.id ? updated : item
        )
      );

      setSuccess(`${promotion.name} was reactivated successfully.`);
    } catch (reactivateError) {
      setError(
        getErrorMessage(
          reactivateError,
          'The promotion could not be reactivated.'
        )
      );
    } finally {
      setCampaignBusy('');
    }
  }
  function toggleProduct(id) {
    setForm((current) => ({ ...current, productIds: current.productIds.includes(id)
      ? current.productIds.filter((item) => item !== id) : [...current.productIds, id] }));
  }

  async function submit(event) {
    event.preventDefault(); setError(''); setSuccess('');
    if (!form.productIds.length) { setError('Select at least one product.'); return; }
    if (form.sendEmail && !window.confirm(`Create this promotion and email every verified customer?`)) return;
    try {
      setSaving(true);
      const created = await promotionService.create(token, {
        ...form,
        discountPercentage: Number(form.discountPercentage),
        startsAt: new Date(form.startsAt).toISOString(),
        endsAt: new Date(form.endsAt).toISOString(),
      });
      setPromotions((current) => [created, ...current]);
      setSuccess(`Promotion created. ${created.emailSuccessCount} email(s) sent${created.emailFailureCount ? `, ${created.emailFailureCount} failed` : ''}.`);
      setForm((current) => ({ ...current, name: '', productIds: [], sendEmail: false }));
    } catch (saveError) { setError(getErrorMessage(saveError)); }
    finally { setSaving(false); }
  }

  return <div className="admin-shell">
    <AdminSidebar user={user} />
    <main className="admin-main">
      <header className="admin-topbar"><div><span>Catalog</span><b>/</b><strong>Promotions</strong></div><Link to="/admin/products"><ArrowLeft size={17}/> Products</Link></header>
      <div className="admin-content promotion-content">
        <section className="admin-page-title"><div><span className="admin-eyebrow">CAMPAIGNS</span><h1>Promotions</h1><p>Create product discounts and notify verified customers by email.</p></div></section>
        {error && <div className="admin-notice promotion-error">{error}</div>}
        {success && <div className="promotion-success"><Check size={17}/>{success}</div>}
        <div className="promotion-layout">
          <form className="promotion-form" onSubmit={submit}>
            <div className="promotion-card"><h2>Campaign details</h2><p>Define the offer and when it is available.</p>
              <label>Promotion name<input required maxLength="160" value={form.name} onChange={(e)=>setForm({...form,name:e.target.value})} placeholder="Summer sale"/></label>
              <div className="promotion-grid"><label>Discount percentage<div className="promotion-percent"><input required type="number" min="1" max="90" value={form.discountPercentage} onChange={(e)=>setForm({...form,discountPercentage:e.target.value})}/><Percent size={16}/></div></label><label>Preview<strong className="discount-preview">$100 → ${(100*(1-Number(form.discountPercentage||0)/100)).toFixed(2)}</strong></label></div>
              <div className="promotion-grid"><label>Starts<input required type="datetime-local" value={form.startsAt} onChange={(e)=>setForm({...form,startsAt:e.target.value})}/></label><label>Ends<input required type="datetime-local" value={form.endsAt} onChange={(e)=>setForm({...form,endsAt:e.target.value})}/></label></div>
            </div>
            <div className="promotion-card"><div className="promotion-card-head"><div><h2>Select products</h2><p>{form.productIds.length} selected</p></div><label className="promotion-search"><Search size={15}/><input value={query} onChange={(e)=>setQuery(e.target.value)} placeholder="Search products"/></label></div>
              <div className="promotion-products">{visibleProducts.map((product)=><label key={product.id} className={form.productIds.includes(product.id)?'selected':''}><input type="checkbox" checked={form.productIds.includes(product.id)} onChange={()=>toggleProduct(product.id)}/><img src={product.imageUrl} alt=""/><span><strong>{product.name}</strong><small>{product.categoryName} · {product.audience}</small></span><b>${Number(product.price).toFixed(2)}</b></label>)}</div>
            </div>
            <div className="promotion-card email-option"><Mail size={22}/><div><strong>Email all verified customers</strong><p>Each customer receives this campaign once. Delivery results are recorded.</p></div><button type="button" className={`admin-switch ${form.sendEmail?'is-on':''}`} onClick={()=>setForm({...form,sendEmail:!form.sendEmail})}><span/><b>{form.sendEmail?'Yes':'No'}</b></button></div>
            <button className="admin-primary-btn promotion-submit" disabled={saving}><Send size={17}/>{saving?'Creating and sending…':'Create promotion'}</button>
          </form>
          <section className="promotion-history"><div className="campaign-history-head"><div><h2>Campaign history</h2><p>{visiblePromotions.length} campaigns shown</p></div><button type="button" onClick={load} disabled={loading}><RefreshCw size={14} className={loading?'spin':''}/></button></div><div className="campaign-history-filters"><label><Search size={13}/><input value={campaignQuery} onChange={(event)=>setCampaignQuery(event.target.value)} placeholder="Search campaigns"/></label><select value={campaignStatus} onChange={(event)=>setCampaignStatus(event.target.value)}>{['All','Active','Scheduled','Expired','Inactive'].map((item)=><option key={item}>{item}</option>)}</select></div>{loading?<p>Loading campaigns…</p>:visiblePromotions.length===0?<p>No promotions match this view.</p>:visiblePromotions.map((item)=>{const now=new Date();const state=!item.isActive?'Inactive':new Date(item.startsAt)>now?'Scheduled':new Date(item.endsAt)<now?'Expired':'Active';return <article key={item.id}><div><strong>{item.name}</strong><span className={state==='Active'?'active':''}>{state}</span></div><b>{Number(item.discountPercentage).toFixed(0)}% off</b><p>{item.productCount} products · {new Date(item.startsAt).toLocaleDateString()} – {new Date(item.endsAt).toLocaleDateString()}</p><small><Mail size={13}/>{item.emailSuccessCount} sent · {item.emailFailureCount} failed</small><div className="campaign-actions"><button type="button" disabled={campaignBusy===item.id} onClick={()=>resendCampaign(item)}><Send size={14}/>{campaignBusy===item.id?'Sending…':'Send pending emails'}</button>{item.isActive ? (
  <button
    type="button"
    className="danger"
    disabled={campaignBusy === item.id}
    onClick={async () => {
      if (!window.confirm(`Deactivate ${item.name}?`)) {
        return;
      }

      try {
        setCampaignBusy(item.id);
        setError('');
        setSuccess('');

        const updated = await promotionService.deactivate(
          token,
          item.id
        );

        setPromotions((all) =>
          all.map((promotion) =>
            promotion.id === item.id ? updated : promotion
          )
        );

        setSuccess(`${item.name} was deactivated successfully.`);
      } catch (deactivateError) {
        setError(
          getErrorMessage(
            deactivateError,
            'The promotion could not be deactivated.'
          )
        );
      } finally {
        setCampaignBusy('');
      }
    }}
  >
    <X size={14}/>
    {campaignBusy === item.id ? 'Deactivating…' : 'Deactivate'}
  </button>
) : (
  <button
    type="button"
    className="reactivate"
    disabled={campaignBusy === item.id}
    onClick={() => reactivateCampaign(item)}
  >
    <RefreshCw size={14}/>
    {campaignBusy === item.id ? 'Reactivating…' : 'Reactivate'}
  </button>
)}</div></article>})}</section>
        </div>
      </div>
    </main>
  </div>;
}



