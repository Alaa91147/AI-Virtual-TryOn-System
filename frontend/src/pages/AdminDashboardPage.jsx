import AdminSidebar from '../components/admin/AdminSidebar.jsx';
import { useCallback, useEffect, useMemo, useState } from 'react';
import { AlertTriangle, ArrowRight, Box, CircleDollarSign, Eye, LayoutDashboard, PackageCheck, Plus, RefreshCw, ShoppingBag, TrendingUp, Users } from 'lucide-react';
import { Link } from 'react-router-dom';
import { useAuth } from '../context/AuthContext.jsx';
import { getErrorMessage } from '../services/authService.js';
import { adminProductService } from '../services/adminProductService.js';
import { customerService } from '../services/customerService.js';
import { orderService } from '../services/orderService.js';
import { promotionService } from '../services/promotionService.js';
import './AdminProductsPage.css';
import './AdminDashboardPage.css';

const orderStatuses = ['Pending', 'Confirmed', 'Processing', 'Shipped', 'Delivered'];
const stockOf = (product) => product.sizes?.reduce((sum, size) => sum + Number(size.stockQuantity || 0), 0) || 0;
const money = (value) => new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD' }).format(Number(value || 0));
const shortDate = (value) => new Intl.DateTimeFormat('en-US', { month: 'short', day: 'numeric' }).format(new Date(value));

export default function AdminDashboardPage() {
  const { token, user } = useAuth();
  const [data, setData] = useState({ products: [], orders: [], customers: [], promotions: [] });
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const load = useCallback(async () => {
    if (!token) return;
    try {
      setLoading(true); setError('');
      const [catalog, orders, customers, promotions] = await Promise.all([adminProductService.getCatalog(token), orderService.getAll(token), customerService.getAll(token), promotionService.getAll(token)]);
      setData({ products: catalog.products || [], orders: orders || [], customers: customers || [], promotions: promotions || [] });
    } catch (requestError) { setError(getErrorMessage(requestError, 'The dashboard could not be loaded.')); }
    finally { setLoading(false); }
  }, [token]);
  useEffect(() => { load(); }, [load]);

  const metrics = useMemo(() => {
    const validOrders = data.orders.filter((order) => order.status !== 'Cancelled');
    return {
      revenue: validOrders.reduce((sum, order) => sum + Number(order.total || 0), 0),
      lowStock: data.products.filter((product) => stockOf(product) < 6),
      activePromotions: data.promotions.filter((promotion) => promotion.isActive && new Date(promotion.endsAt) >= new Date()),
    };
  }, [data]);
  const revenueDays = useMemo(() => {
    const days = Array.from({ length: 7 }, (_, index) => { const date = new Date(); date.setHours(0, 0, 0, 0); date.setDate(date.getDate() - (6 - index)); return { date, label: date.toLocaleDateString('en-US', { weekday: 'short' }), value: 0 }; });
    data.orders.filter((order) => order.status !== 'Cancelled').forEach((order) => { const created = new Date(order.createdAt); created.setHours(0, 0, 0, 0); const match = days.find((day) => day.date.getTime() === created.getTime()); if (match) match.value += Number(order.total || 0); });
    return days;
  }, [data.orders]);
  const maxRevenue = Math.max(...revenueDays.map((day) => day.value), 1);
  const nextCampaign = [...metrics.activePromotions].sort((a, b) => new Date(a.endsAt) - new Date(b.endsAt))[0];

  return <div className="admin-shell">
    <AdminSidebar user={user} />
    <main className="admin-main"><header className="admin-topbar"><div><span>Workspace</span><b>/</b><strong>Overview</strong></div><Link to="/shop"><Eye size={17}/> View storefront</Link></header><div className="admin-content dashboard-content">
      <section className="admin-page-title dashboard-title"><div><span className="admin-eyebrow">ADMIN OVERVIEW</span><h1>Good day, {user?.fullName?.split(' ')[0] || 'Admin'}.</h1><p>Here is what is happening across your store today.</p></div><div><button className="admin-secondary-btn" onClick={load} disabled={loading}><RefreshCw size={16} className={loading ? 'spin' : ''}/> Refresh</button><Link className="admin-primary-btn" to="/admin/products"><Plus size={17}/> Add product</Link></div></section>
      {error && <div className="admin-notice dashboard-error"><span>{error}</span><button onClick={load}>Try again</button></div>}
      <section className="admin-stats dashboard-stats"><article><span><CircleDollarSign size={20}/></span><div><small>Total revenue</small><strong>{loading ? '—' : money(metrics.revenue)}</strong><p>excluding cancelled orders</p></div></article><article><span><Box size={20}/></span><div><small>Orders</small><strong>{loading ? '—' : data.orders.length}</strong><p><b>{data.orders.filter((order)=>order.status==='Pending').length}</b> awaiting action</p></div></article><article><span><Users size={20}/></span><div><small>Customers</small><strong>{loading ? '—' : data.customers.filter((customer)=>customer.role==='Customer').length}</strong><p>{data.customers.filter((customer)=>customer.isEmailVerified).length} verified</p></div></article><article><span><AlertTriangle size={20}/></span><div><small>Low stock</small><strong>{loading ? '—' : metrics.lowStock.length}</strong><p>products below 6 units</p></div></article></section>
      <div className="dashboard-grid">
        <section className="dashboard-card revenue-card"><header><div><span className="admin-eyebrow">PERFORMANCE</span><h2>Revenue · last 7 days</h2></div><TrendingUp size={21}/></header><div className="revenue-chart">{revenueDays.map((day)=><div key={day.label}><span title={money(day.value)} style={{height:`${Math.max(day.value/maxRevenue*100,4)}%`}}/><small>{day.label}</small><b>{day.value ? money(day.value).replace('.00','') : '—'}</b></div>)}</div></section>
        <section className="dashboard-card pipeline-card"><header><div><span className="admin-eyebrow">FULFILLMENT</span><h2>Order pipeline</h2></div><Link to="/admin/orders">Manage <ArrowRight size={14}/></Link></header><div>{orderStatuses.map((status)=>{const count=data.orders.filter((order)=>order.status===status).length;const percent=data.orders.length?count/data.orders.length*100:0;return <article key={status}><div><span>{status}</span><b>{count}</b></div><i><em style={{width:`${percent}%`}}/></i></article>})}</div></section>
        <section className="dashboard-card recent-card"><header><div><span className="admin-eyebrow">LATEST ACTIVITY</span><h2>Recent orders</h2></div><Link to="/admin/orders">View all <ArrowRight size={14}/></Link></header>{loading?<p className="dashboard-empty">Loading orders…</p>:data.orders.slice(0,5).map((order)=><article key={order.id}><div><strong>{order.orderNumber}</strong><small>{order.customerName} · {shortDate(order.createdAt)}</small></div><span className={`dashboard-status status-${order.status.toLowerCase()}`}>{order.status}</span><b>{money(order.total)}</b></article>)}{!loading&&!data.orders.length&&<p className="dashboard-empty">No orders have been placed yet.</p>}</section>
        <section className="dashboard-card inventory-card"><header><div><span className="admin-eyebrow">INVENTORY</span><h2>Stock attention</h2></div><Link to="/admin/products">Inventory <ArrowRight size={14}/></Link></header>{metrics.lowStock.slice(0,5).map((product)=><article key={product.id}><img src={product.imageUrl} alt=""/><div><strong>{product.name}</strong><small>{product.categoryName}</small></div><b className={stockOf(product)===0?'out':''}>{stockOf(product)} left</b></article>)}{!loading&&!metrics.lowStock.length&&<p className="dashboard-empty"><PackageCheck size={20}/> Inventory levels look healthy.</p>}</section>
      </div>
      <section className="dashboard-card campaign-strip"><div><span><CircleDollarSign size={20}/></span><div><strong>{metrics.activePromotions.length} active campaign{metrics.activePromotions.length===1?'':'s'}</strong><small>{nextCampaign ? `Next ending ${shortDate(nextCampaign.endsAt)}` : 'Create a campaign to drive product discovery.'}</small></div></div><Link className="admin-secondary-btn" to="/admin/promotions">Manage promotions <ArrowRight size={15}/></Link></section>
    </div></main>
  </div>;
}



