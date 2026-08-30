import { useEffect, useMemo, useState } from 'react';
import { Box, ChevronDown, Eye, PackageCheck, Search, ShoppingBag, Truck, Users } from 'lucide-react';
import { Link } from 'react-router-dom';
import { useAuth } from '../context/AuthContext.jsx';
import { orderService } from '../services/orderService.js';
import { getErrorMessage } from '../services/authService.js';
import './AdminProductsPage.css';
import './AdminOrdersPage.css';
import AdminSidebar from '../components/admin/AdminSidebar.jsx';
const statuses = ['Pending', 'Confirmed', 'Processing', 'Shipped', 'Delivered', 'Cancelled'];

export default function AdminOrdersPage() {
  const { token, user } = useAuth();
  const [orders, setOrders] = useState([]);
  const [query, setQuery] = useState('');
  const [filter, setFilter] = useState('All');
  const [expanded, setExpanded] = useState('');
  const [busy, setBusy] = useState('');
  const [error, setError] = useState('');
  useEffect(() => { orderService.getAll(token).then(setOrders).catch((e)=>setError(getErrorMessage(e))); }, [token]);
  const visible = useMemo(() => orders.filter((order) => (filter === 'All' || order.status === filter) && `${order.orderNumber} ${order.customerName} ${order.customerEmail}`.toLowerCase().includes(query.toLowerCase())), [orders, query, filter]);
  async function changeStatus(order, status) {
    if (status === 'Cancelled' && !window.confirm('Cancel this order and restore its stock?')) return;
    try { setBusy(order.id); setError(''); const updated = await orderService.updateStatus(token, order.id, status, order.trackingNumber || ''); setOrders((all) => all.map((item) => item.id === updated.id ? updated : item)); }
    catch (e) { setError(getErrorMessage(e, 'Could not update the order.')); } finally { setBusy(''); }
  }
  const revenue = orders.filter((o)=>o.status!=='Cancelled').reduce((sum,o)=>sum+Number(o.total),0);
  return <div className="admin-shell"><AdminSidebar user={user} counts={{ '/admin/orders': orders.length }}/>
    <main className="admin-main"><header className="admin-topbar"><div><span>Sales</span><b>/</b><strong>Orders</strong></div></header><div className="admin-content"><section className="admin-page-title"><div><span className="admin-eyebrow">ORDER MANAGEMENT</span><h1>Orders</h1><p>Review purchases, update fulfillment, and track customer orders.</p></div></section>{error&&<div className="admin-notice">{error}</div>}
    <section className="admin-stats order-stats"><article><span><Box size={20}/></span><div><small>Total orders</small><strong>{orders.length}</strong><p>all customer orders</p></div></article><article><span><PackageCheck size={20}/></span><div><small>Pending</small><strong>{orders.filter(o=>o.status==='Pending').length}</strong><p>need confirmation</p></div></article><article><span><Truck size={20}/></span><div><small>In fulfillment</small><strong>{orders.filter(o=>['Confirmed','Processing','Shipped'].includes(o.status)).length}</strong><p>active orders</p></div></article><article><span><PackageCheck size={20}/></span><div><small>Order value</small><strong>${revenue.toFixed(2)}</strong><p>excluding cancelled</p></div></article></section>
    <section className="admin-products-panel"><div className="admin-panel-toolbar"><div className="admin-status-tabs">{['All',...statuses].map(s=><button key={s} className={filter===s?'is-active':''} onClick={()=>setFilter(s)}>{s}</button>)}</div><div className="admin-toolbar-actions"><label><Search size={17}/><input value={query} onChange={e=>setQuery(e.target.value)} placeholder="Order or customerâ€¦"/></label></div></div><div className="admin-table-wrap"><table className="admin-products-table admin-orders-table"><thead><tr><th>Order</th><th>Customer</th><th>Date</th><th>Items</th><th>Total</th><th>Status</th><th/></tr></thead><tbody>{visible.length===0?<tr><td colSpan="7" className="admin-empty">No orders in this view.</td></tr>:visible.map(order=><OrderRow key={order.id} order={order} busy={busy===order.id} expanded={expanded===order.id} onExpand={()=>setExpanded(expanded===order.id?'':order.id)} onStatus={changeStatus}/>)}</tbody></table></div></section></div></main></div>;
}

function OrderRow({order,busy,expanded,onExpand,onStatus}) {
  return <><tr><td><strong>{order.orderNumber}</strong><small>{order.status==='Pending'?'New order':'Updated'}</small></td><td><strong>{order.customerName}</strong><small>{order.customerEmail}</small></td><td><strong>{new Date(order.createdAt).toLocaleDateString()}</strong><small>{new Date(order.createdAt).toLocaleTimeString([], {hour:'2-digit',minute:'2-digit'})}</small></td><td><strong>{order.items.reduce((s,i)=>s+i.quantity,0)} items</strong><small>{order.items.length} products</small></td><td><strong>${Number(order.total).toFixed(2)}</strong><small>Subtotal ${Number(order.subtotal).toFixed(2)}</small></td><td><label className={`order-status-select status-${order.status.toLowerCase()}`}><select disabled={busy||order.status==='Cancelled'} value={order.status} onChange={e=>onStatus(order,e.target.value)}>{statuses.map(s=><option key={s}>{s}</option>)}</select><ChevronDown size={14}/></label></td><td><button className="order-view" onClick={onExpand}><Eye size={17}/></button></td></tr>{expanded&&<tr className="order-detail-row"><td colSpan="7"><div className="order-detail-items">{order.items.map(item=><article key={item.id}><img src={item.imageUrl} alt=""/><span><strong>{item.productName}</strong><small>{item.sizeName}{item.colorName?` Â· ${item.colorName}`:''} Â· Qty {item.quantity}</small></span><b>${Number(item.lineTotal).toFixed(2)}</b></article>)}</div></td></tr>}</>;
}



