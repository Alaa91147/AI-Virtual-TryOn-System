import { useCallback, useEffect, useMemo, useState } from 'react';
import { Bell, Box, ChevronDown, Download, Eye, LayoutDashboard, MailCheck, PackageCheck, RefreshCw, Search, ShoppingBag, UserCheck, Users } from 'lucide-react';
import { Link } from 'react-router-dom';
import { useAuth } from '../context/AuthContext.jsx';
import AdminSidebar from '../components/admin/AdminSidebar.jsx';
import { getErrorMessage } from '../services/authService.js';
import { customerService } from '../services/customerService.js';
import './AdminProductsPage.css';
import './AdminCustomersPage.css';

export default function AdminCustomersPage() {
  const { token, user } = useAuth();
  const [customers, setCustomers] = useState([]);
  const [query, setQuery] = useState('');
  const [filter, setFilter] = useState('All');
  const [expanded, setExpanded] = useState('');
  const [busy, setBusy] = useState('');
  const [messageCustomer, setMessageCustomer] = useState(null);
  const [error, setError] = useState('');
  const [loadError, setLoadError] = useState('');
  const [loading, setLoading] = useState(true);

  const loadCustomers = useCallback(async ({ silent = false, retries = 2 } = {}) => {
    if (!token) return;
    if (!silent) setLoading(true);
    setLoadError('');
    for (let attempt = 0; attempt <= retries; attempt += 1) {
      try {
        const data = await customerService.getAll(token);
        setCustomers(Array.isArray(data) ? data : []);
        setLoading(false);
        return;
      } catch (requestError) {
        if (attempt === retries) {
          setLoadError(getErrorMessage(requestError, 'Could not load customers.'));
          setLoading(false);
          return;
        }
        await new Promise((resolve) => window.setTimeout(resolve, 700 * (attempt + 1)));
      }
    }
  }, [token]);

  useEffect(() => { loadCustomers(); }, [loadCustomers]);
  useEffect(() => {
    const refreshWhenVisible = () => {
      if (document.visibilityState === 'visible') loadCustomers({ silent: true, retries: 1 });
    };
    document.addEventListener('visibilitychange', refreshWhenVisible);
    return () => document.removeEventListener('visibilitychange', refreshWhenVisible);
  }, [loadCustomers]);

  const visible = useMemo(() => customers.filter((customer) => {
    const matchesFilter = filter === 'All' || customer.role === filter ||
      (filter === 'Verified' && customer.isEmailVerified) || (filter === 'Google' && customer.usesGoogle);
    return matchesFilter && `${customer.fullName} ${customer.email}`.toLowerCase().includes(query.toLowerCase());
  }), [customers, filter, query]);

  async function changeRole(customer, role) {
    if (customer.id === user?.id && role !== 'Admin') {
      setError('You cannot remove your own administrator access.');
      return;
    }
    if (!window.confirm(`Change ${customer.fullName}'s role to ${role}?`)) return;
    try {
      setBusy(customer.id); setError('');
      const updated = await customerService.updateRole(token, customer.id, role);
      setCustomers((items) => items.map((item) => item.id === updated.id ? updated : item));
    } catch (e) { setError(getErrorMessage(e, 'Could not change this role.')); }
    finally { setBusy(''); }
  }

  const customerAccounts = customers.filter((item) => item.role === 'Customer');
  const totalSpent = customerAccounts.reduce((sum, item) => sum + Number(item.totalSpent), 0);

  function exportCustomers() {
    if (!visible.length) { setError('There are no customers to export.'); return; }
    const escapeCsv = (value) => `"${String(value ?? '').replace(/"/g, '""')}"`;
    const rows = visible.map((customer) => [customer.fullName, customer.email, customer.role,
      customer.isEmailVerified ? 'Verified' : 'Not verified', customer.usesGoogle ? 'Google' : 'Email',
      customer.orderCount, customer.totalSpent, customer.phoneNumber || '', customer.address || '', customer.createdAt]);
    const csv = [['Name','Email','Role','Verification','Sign in','Orders','Total spent','Phone','Address','Joined'], ...rows]
      .map((row) => row.map(escapeCsv).join(',')).join('\n');
    const url = URL.createObjectURL(new Blob([`\uFEFF${csv}`], { type: 'text/csv;charset=utf-8' }));
    const link = document.createElement('a'); link.href = url;
    link.download = `customers-${new Date().toISOString().slice(0,10)}.csv`;
    document.body.appendChild(link); link.click(); link.remove(); URL.revokeObjectURL(url);
  }

  return <div className="admin-shell">
    <AdminSidebar user={user} counts={{ '/admin/customers': customerAccounts.length }} />
    <main className="admin-main"><header className="admin-topbar"><div><span>Community</span><b>/</b><strong>Customers</strong></div></header><div className="admin-content">
      <section className="admin-page-title"><div><span className="admin-eyebrow">CUSTOMER MANAGEMENT</span><h1>Customers</h1><p>Understand your audience, review activity, and manage customer access.</p></div><button type="button" className="admin-primary-btn" onClick={exportCustomers} disabled={!visible.length}><Download size={16}/> Export CSV</button></section>
      {(loadError || error) && <div className="admin-notice customer-load-error"><span>{loadError || error}</span>{loadError&&<button className="admin-secondary-btn" onClick={()=>loadCustomers()}><RefreshCw size={15}/> Try again</button>}</div>}
      <section className="admin-stats customer-stats"><article><span><Users size={20}/></span><div><small>Customers</small><strong>{customerAccounts.length}</strong><p>registered accounts</p></div></article><article><span><MailCheck size={20}/></span><div><small>Verified</small><strong>{customerAccounts.filter(c=>c.isEmailVerified).length}</strong><p>verified emails</p></div></article><article><span><UserCheck size={20}/></span><div><small>With orders</small><strong>{customerAccounts.filter(c=>c.orderCount>0).length}</strong><p>paying customers</p></div></article><article><span><Box size={20}/></span><div><small>Customer value</small><strong>${totalSpent.toFixed(2)}</strong><p>excluding cancelled orders</p></div></article></section>
      <section className="admin-products-panel"><div className="admin-panel-toolbar"><div className="admin-status-tabs">{['All','Customer','Admin','Verified','Google'].map(item=><button key={item} className={filter===item?'is-active':''} onClick={()=>setFilter(item)}>{item}</button>)}</div><div className="admin-toolbar-actions"><label><Search size={17}/><input value={query} onChange={(e)=>setQuery(e.target.value)} placeholder="Search customers..."/></label></div></div>
        <div className="admin-table-wrap"><table className="admin-products-table admin-customers-table"><thead><tr><th>Customer</th><th>Joined</th><th>Orders</th><th>Total spent</th><th>Sign in</th><th>Role</th><th/></tr></thead><tbody>{loading?<tr><td colSpan="7" className="admin-empty customer-loading"><span className="spinner"/> Loading customers...</td></tr>:loadError?<tr><td colSpan="7" className="admin-empty">Customer data could not be loaded. Use Try again above.</td></tr>:visible.length===0?<tr><td colSpan="7" className="admin-empty">No customers match this filter.</td></tr>:visible.map(customer=><CustomerRows key={customer.id} customer={customer} busy={busy===customer.id} expanded={expanded===customer.id} onExpand={()=>setExpanded(expanded===customer.id?'':customer.id)} onRole={changeRole} onMessage={()=>setMessageCustomer(customer)}/>)}</tbody></table></div>
      </section>
    </div></main>
    {messageCustomer && <NotificationModal customer={messageCustomer} token={token} onClose={()=>setMessageCustomer(null)} onError={setError}/>} 
  </div>;
}

function CustomerRows({ customer, busy, expanded, onExpand, onRole, onMessage }) {
  const initials = customer.fullName?.split(' ').map(part=>part[0]).join('').slice(0,2).toUpperCase() || 'CU';
  return <><tr><td><div className="customer-identity"><span>{initials}</span><div><strong>{customer.fullName}</strong><small>{customer.email}</small></div></div></td><td><strong>{new Date(customer.createdAt).toLocaleDateString()}</strong><small>{customer.isEmailVerified?'Verified':'Not verified'}</small></td><td><strong>{customer.orderCount}</strong><small>{customer.lastOrderAt?`Last ${new Date(customer.lastOrderAt).toLocaleDateString()}`:'No orders yet'}</small></td><td><strong>${Number(customer.totalSpent).toFixed(2)}</strong><small>completed value</small></td><td><span className={`customer-provider ${customer.usesGoogle?'google':''}`}>{customer.usesGoogle?'Google':'Email'}</span></td><td><label className="customer-role-select"><select disabled={busy} value={customer.role} onChange={(e)=>onRole(customer,e.target.value)}><option>Customer</option><option>Admin</option></select><ChevronDown size={14}/></label></td><td><button className="order-view" onClick={onExpand} title="View details"><Eye size={17}/></button></td></tr>
    {expanded&&<tr className="customer-detail-row"><td colSpan="7"><div className="customer-detail"><div><small>PHONE</small><strong>{customer.phoneNumber||'Not provided'}</strong></div><div><small>DELIVERY ADDRESS</small><strong>{customer.address||'Not provided'}</strong></div><div><small>AUDIENCE</small><strong>{customer.shoppingPreference||customer.gender||'Not selected'}</strong></div><button className="admin-secondary-btn" onClick={onMessage}><Bell size={16}/> Send notification</button></div></td></tr>}</>;
}

function NotificationModal({ customer, token, onClose, onError }) {
  const [title,setTitle]=useState('A message from Virtual Try-On');
  const [message,setMessage]=useState('');
  const [sending,setSending]=useState(false);
  async function submit(e){e.preventDefault();try{setSending(true);await customerService.sendNotification(token,customer.id,title,message);onClose();}catch(error){onError(getErrorMessage(error,'Could not send the notification.'));}finally{setSending(false);}}
  return <div className="customer-modal-backdrop" onMouseDown={onClose}><form className="customer-modal" onSubmit={submit} onMouseDown={(e)=>e.stopPropagation()}><span className="admin-eyebrow">CUSTOMER MESSAGE</span><h2>Notify {customer.fullName}</h2><p>The message will appear in this customerâ€™s notification center.</p><label>Title<input value={title} maxLength="100" onChange={(e)=>setTitle(e.target.value)} required/></label><label>Message<textarea value={message} maxLength="500" rows="5" onChange={(e)=>setMessage(e.target.value)} required placeholder="Write your message..."/></label><div><button type="button" className="admin-secondary-btn" onClick={onClose}>Cancel</button><button className="admin-primary-btn" disabled={sending}>{sending?'Sending...':'Send notification'}</button></div></form></div>;
}




