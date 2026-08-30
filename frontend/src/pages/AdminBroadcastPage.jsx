import AdminSidebar from '../components/admin/AdminSidebar.jsx';
import { useState } from 'react';
import { Bell, Box, CircleDollarSign, LayoutDashboard, Megaphone, Send, ShoppingBag, Users } from 'lucide-react';
import { Link } from 'react-router-dom';
import { useAuth } from '../context/AuthContext.jsx';
import { getErrorMessage } from '../services/authService.js';
import { notificationService } from '../services/notificationService.js';
import './AdminProductsPage.css';
import './AdminManagement.css';

export default function AdminBroadcastPage(){
 const {token,user}=useAuth();const [form,setForm]=useState({title:'',message:'',type:'announcement',link:'/shop',audience:'All'});const [saving,setSaving]=useState(false);const [result,setResult]=useState(null);const [error,setError]=useState('');
 async function submit(event){event.preventDefault();try{setSaving(true);setError('');setResult(null);const response=await notificationService.broadcast(token,form);setResult(response);setForm((current)=>({...current,title:'',message:''}));}catch(e){setError(getErrorMessage(e,'The broadcast could not be sent.'));}finally{setSaving(false);}}
 return <div className="admin-shell"><AdminNav user={user}/><main className="admin-main"><header className="admin-topbar"><div><span>Communication</span><b>/</b><strong>Broadcast</strong></div></header><div className="admin-content management-content"><section className="admin-page-title"><div><span className="admin-eyebrow">REAL-TIME COMMUNICATION</span><h1>Broadcast notification</h1><p>Send an instant, persistent notification to the selected audience.</p></div></section>{error&&<div className="admin-notice">{error}</div>}{result&&<div className="management-success"><Bell size={17}/>{result.recipientCount} account(s) received this notification.</div>}<form className="management-form" onSubmit={submit}><section><h2>Message</h2><p>This appears immediately for connected users and remains in their notification history.</p><label>Title<input required minLength="2" maxLength="100" value={form.title} onChange={(e)=>setForm({...form,title:e.target.value})} placeholder="Important store update"/></label><label>Message<textarea required minLength="2" maxLength="500" rows="6" value={form.message} onChange={(e)=>setForm({...form,message:e.target.value})} placeholder="Write the announcementâ€¦"/></label><div className="management-grid"><label>Audience<select value={form.audience} onChange={(e)=>setForm({...form,audience:e.target.value})}><option>All</option><option>Customer</option><option>Admin</option></select></label><label>Type<select value={form.type} onChange={(e)=>setForm({...form,type:e.target.value})}><option value="announcement">Announcement</option><option value="promotion">Promotion</option><option value="system">System</option></select></label></div><label>Open link<input maxLength="2048" value={form.link} onChange={(e)=>setForm({...form,link:e.target.value})} placeholder="/shop"/></label><button className="admin-primary-btn management-submit" disabled={saving}><Send size={16}/>{saving?'Sendingâ€¦':'Send broadcast'}</button></section><aside><Megaphone size={26}/><h3>Delivery behavior</h3><ul><li>Online users see the message instantly.</li><li>Offline users see it when they return.</li><li>Every recipient receives an unread notification.</li><li>The action is recorded in Admin Audit Logs.</li></ul></aside></form></div></main></div>;
}

function AdminNav({user}){return <AdminSidebar user={user} />}



