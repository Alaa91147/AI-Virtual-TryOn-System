import {
  Building2,
  CalendarDays,
  Camera,
  Edit3,
  Eye,
  Footprints,
  Heart,
  Image,
  LogOut,
  Mail,
  MapPin,
  Package,
  Phone,
  Ruler,
  Save,
  Scale,
  ShieldCheck,
  Shirt,
  SlidersHorizontal,
  Trash2,
  Upload,
  UserRound,
  X,
} from 'lucide-react';
import { useState } from 'react';
import ImageAdjuster from '../components/ImageAdjuster.jsx';
import { getErrorMessage } from '../services/authService.js';
import { useAuth } from '../context/AuthContext.jsx';
import {
  getImageValidationRequirement,
  validateImageUploadBasics,
  validateProfileImageUpload,
} from '../services/imageValidationService.js';

const emptyForm = {
  fullName: '',
  gender: '',
  dateOfBirth: '',
  phoneNumber: '',
  deliveryCountry: '',
  deliveryCity: '',
  deliveryStreet: '',
  deliveryBuilding: '',
  deliveryPhoneNumber: '',
  profilePhotoUrl: '',
  fullBodyPhotoUrl: '',
  upperBodyPhotoUrl: '',
  lowerBodyPhotoUrl: '',
  facePhotoUrl: '',
  heightCm: '',
  weightKg: '',
  preferredSize: '',
  bodyShape: '',
  shoeSize: '',
  topSize: '',
  bottomSize: '',
};

function formatDate(value) {
  if (!value) {
    return 'Not added';
  }

  return new Intl.DateTimeFormat(undefined, {
    year: 'numeric',
    month: 'short',
    day: 'numeric',
  }).format(new Date(value));
}

function formatGender(value) {
  if (!value) {
    return 'Not added';
  }

  return value.charAt(0).toUpperCase() + value.slice(1);
}

function displayValue(value, suffix = '') {
  if (value === null || value === undefined || value === '') {
    return 'Not added';
  }

  return `${value}${suffix}`;
}

function toInputDate(value) {
  return value ? value.slice(0, 10) : '';
}

function toFormValues(user) {
  return {
    fullName: user?.fullName || '',
    gender: user?.gender || '',
    dateOfBirth: toInputDate(user?.dateOfBirth),
    phoneNumber: user?.phoneNumber || '',
    deliveryCountry: user?.deliveryCountry || '',
    deliveryCity: user?.deliveryCity || '',
    deliveryStreet: user?.deliveryStreet || '',
    deliveryBuilding: user?.deliveryBuilding || '',
    deliveryPhoneNumber: user?.deliveryPhoneNumber || '',
    profilePhotoUrl: user?.profilePhotoUrl || '',
    fullBodyPhotoUrl: user?.fullBodyPhotoUrl || '',
    upperBodyPhotoUrl: user?.upperBodyPhotoUrl || '',
    lowerBodyPhotoUrl: user?.lowerBodyPhotoUrl || '',
    facePhotoUrl: user?.facePhotoUrl || '',
    heightCm: user?.heightCm ?? '',
    weightKg: user?.weightKg ?? '',
    preferredSize: user?.preferredSize || '',
    bodyShape: user?.bodyShape || '',
    shoeSize: user?.shoeSize || '',
    topSize: user?.topSize || '',
    bottomSize: user?.bottomSize || '',
  };
}

function normalizeNumber(value) {
  return value === '' ? null : Number(value);
}

const tryOnPhotos = [
  {
    key: 'fullBodyPhotoUrl',
    title: 'Full body photo',
    description: 'Upload a clothed full body photo for complete try-on',
  },
  {
    key: 'upperBodyPhotoUrl',
    title: 'Upper body photo',
    description: 'Upload a clothed upper body photo for t-shirts, shirts, and tops',
  },
  {
    key: 'lowerBodyPhotoUrl',
    title: 'Lower body photo',
    description: 'Upload a clothed lower body photo for pants and bottoms',
  },
  {
    key: 'facePhotoUrl',
    title: 'Face photo',
    description: 'Upload a clear face photo for caps and accessories',
  },
];

const photoLabels = Object.fromEntries([
  ['profilePhotoUrl', 'profile photo'],
  ...tryOnPhotos.map((photo) => [photo.key, photo.title.toLowerCase()]),
]);

export default function ProfilePage() {
  const { user, logout, updateProfile } = useAuth();
  const [editScope, setEditScope] = useState(null);
  const [values, setValues] = useState(() => toFormValues(user));
  const [formError, setFormError] = useState('');
  const [photoErrors, setPhotoErrors] = useState({});
  const [success, setSuccess] = useState('');
  const [saving, setSaving] = useState(false);
  const [pendingPhoto, setPendingPhoto] = useState(null);
  const [viewedPhoto, setViewedPhoto] = useState(null);

  const editing = editScope !== null;
  const editingAccount = editScope === 'all' || editScope === 'account';
  const editingAddress = editScope === 'all' || editScope === 'address';
  const editingFit = editScope === 'all' || editScope === 'fit';
  const editingPhotos = editScope === 'all' || editScope === 'photos';

  function beginEdit(scope = 'all') {
    setValues(toFormValues(user));
    setFormError('');
    setPhotoErrors({});
    setSuccess('');
    setEditScope(scope);
  }

  function cancelEdit() {
    setValues(toFormValues(user));
    setFormError('');
    setPhotoErrors({});
    setEditScope(null);
  }

  function updateValue(event) {
    const { name, value } = event.target;
    setValues((current) => ({
      ...current,
      [name]: value,
    }));
    setFormError('');
    setSuccess('');
  }

  async function updatePhotoField(fieldName, file) {
    if (!file) {
      return;
    }

    setFormError('');
    setSuccess('');
    setPhotoErrors((current) => ({
      ...current,
      [fieldName]: '',
    }));

    try {
      await validateImageUploadBasics(file);
    } catch (error) {
      setPhotoErrors((current) => ({
        ...current,
        [fieldName]: error instanceof Error ? error.message : 'This photo is not valid.',
      }));
      return;
    }

    setPendingPhoto({ fieldName, file });
  }

  async function applyAdjustedPhoto(file) {
    if (!pendingPhoto) return;

    const { fieldName } = pendingPhoto;
    await validateProfileImageUpload(fieldName, file);

    const reader = new FileReader();
    const dataUrl = await new Promise((resolve, reject) => {
      reader.onload = () => resolve(String(reader.result || ''));
      reader.onerror = () => reject(new Error('Could not process the adjusted image.'));
      reader.readAsDataURL(file);
    });

    setValues((current) => ({
      ...current,
      [fieldName]: dataUrl,
    }));
    setPhotoErrors((current) => ({ ...current, [fieldName]: '' }));
    setPendingPhoto(null);
  }

  function updateProfilePhoto(event) {
    const file = event.target.files?.[0];
    if (!file) {
      return;
    }

    updatePhotoField('profilePhotoUrl', file);
    event.target.value = '';
  }

  function updateTryOnPhoto(fieldName, event) {
    updatePhotoField(fieldName, event.target.files?.[0]);
    event.target.value = '';
  }

  async function adjustExistingPhoto(fieldName, source) {
    if (!source) return;

    setPhotoErrors((current) => ({ ...current, [fieldName]: '' }));
    try {
      const response = await fetch(source);
      if (!response.ok) throw new Error('Could not open this image for editing.');
      const blob = await response.blob();
      const extension = blob.type === 'image/png' ? 'png' : blob.type === 'image/webp' ? 'webp' : 'jpg';
      const file = new File([blob], `${fieldName}.${extension}`, {
        type: blob.type || 'image/jpeg',
      });
      await validateImageUploadBasics(file);

      if (!editingPhotos) {
        beginEdit('photos');
      }
      setPendingPhoto({ fieldName, file });
    } catch (error) {
      setPhotoErrors((current) => ({
        ...current,
        [fieldName]: error instanceof Error ? error.message : 'Could not edit this image.',
      }));
    }
  }

  function removeTryOnPhoto(fieldName) {
    setValues((current) => ({
      ...current,
      [fieldName]: '',
    }));
    setPhotoErrors((current) => ({
      ...current,
      [fieldName]: '',
    }));
  }

  async function saveChanges() {
    if (!values.fullName.trim()) {
      setFormError('Full name is required.');
      return;
    }

    try {
      setSaving(true);
      setFormError('');
      await updateProfile({
        fullName: values.fullName.trim(),
        gender: values.gender,
        dateOfBirth: values.dateOfBirth || null,
        phoneNumber: values.phoneNumber || null,
        deliveryCountry: values.deliveryCountry || null,
        deliveryCity: values.deliveryCity || null,
        deliveryStreet: values.deliveryStreet || null,
        deliveryBuilding: values.deliveryBuilding || null,
        deliveryPhoneNumber: values.deliveryPhoneNumber || null,
        profilePhotoUrl: values.profilePhotoUrl || null,
        fullBodyPhotoUrl: values.fullBodyPhotoUrl || null,
        upperBodyPhotoUrl: values.upperBodyPhotoUrl || null,
        lowerBodyPhotoUrl: values.lowerBodyPhotoUrl || null,
        facePhotoUrl: values.facePhotoUrl || null,
        heightCm: normalizeNumber(values.heightCm),
        weightKg: normalizeNumber(values.weightKg),
        preferredSize: values.preferredSize || null,
        bodyShape: values.bodyShape || null,
        shoeSize: values.shoeSize || null,
        topSize: values.topSize || null,
        bottomSize: values.bottomSize || null,
      });
      setSuccess('Profile updated successfully.');
      setEditScope(null);
    } catch (error) {
      setFormError(getErrorMessage(error, 'Could not update profile.'));
    } finally {
      setSaving(false);
    }
  }

  function handleSubmit(event) {
    event.preventDefault();
    saveChanges();
  }

  function renderSectionActions(scope, label) {
    if (!editing) {
      return (
        <button className="ghost-button compact-button section-edit-button" type="button" onClick={() => beginEdit(scope)}>
          <Edit3 size={16} aria-hidden="true" />
          <span>Edit {label}</span>
        </button>
      );
    }

    if (editScope !== scope) {
      return null;
    }

    return (
      <div className="section-edit-actions">
        <button className="ghost-button compact-button" type="button" onClick={cancelEdit} disabled={saving}>
          <X size={16} aria-hidden="true" />
          <span>Cancel</span>
        </button>
        <button className="primary-button compact-button" type="button" onClick={saveChanges} disabled={saving}>
          <Save size={16} aria-hidden="true" />
          <span>{saving ? 'Saving…' : 'Save'}</span>
        </button>
      </div>
    );
  }

  const photoUrl = editingAccount ? values.profilePhotoUrl : user?.profilePhotoUrl;
  const activeFitValues = editingFit ? values : user;
  const completedFitFields = [
    activeFitValues?.heightCm,
    activeFitValues?.weightKg,
    activeFitValues?.preferredSize,
    activeFitValues?.bodyShape,
    activeFitValues?.shoeSize,
    activeFitValues?.topSize,
    activeFitValues?.bottomSize,
  ].filter((value) => value !== null && value !== undefined && value !== '').length;
  const activeAddressValues = editingAddress ? values : user;
  const completedAddressFields = [
    activeAddressValues?.deliveryCountry,
    activeAddressValues?.deliveryCity,
    activeAddressValues?.deliveryStreet,
    activeAddressValues?.deliveryBuilding,
    activeAddressValues?.deliveryPhoneNumber,
  ].filter(Boolean).length;
  const activePhotoValues = editingPhotos ? values : user;
  const completedTryOnPhotos = tryOnPhotos.filter((photo) => Boolean(activePhotoValues?.[photo.key])).length;

  return (
    <main className="profile-shell">
      <header className="profile-topbar">
        <div className="profile-title-block">
          <span className="auth-kicker">AI Virtual Try-On Shop</span>
          <h1>Your profile</h1>
          <p>Manage your personal details, sizing information, and try-on photos.</p>
        </div>
        <div className="profile-topbar-actions">
          {editScope === 'all' ? (
            <>
              <button className="secondary-button profile-header-button" type="button" onClick={cancelEdit} disabled={saving}>
                <X size={17} aria-hidden="true" />
                <span>Cancel</span>
              </button>
              <button
                className="primary-button profile-header-button"
                type="button"
                onClick={saveChanges}
                disabled={saving}
              >
                <Save size={17} aria-hidden="true" />
                <span>{saving ? 'Saving…' : 'Save changes'}</span>
              </button>
            </>
          ) : !editing ? (
            <button className="primary-button profile-header-button" type="button" onClick={() => beginEdit('all')}>
              <Edit3 size={17} aria-hidden="true" />
              <span>Edit all</span>
            </button>
          ) : <span className="profile-editing-indicator">Editing one section</span>}
          <button className="ghost-button profile-header-button" type="button" onClick={logout}>
            <LogOut size={18} aria-hidden="true" />
            <span>Logout</span>
          </button>
        </div>
      </header>

      <section className="profile-dashboard">
        <aside className="profile-overview">
          <div className="profile-overview-main">
            <div className={photoUrl ? 'avatar avatar-photo profile-avatar-large' : 'avatar profile-avatar-large'} aria-hidden="true">
              {photoUrl ? <img src={photoUrl} alt="" /> : user?.fullName?.charAt(0).toUpperCase() || 'U'}
            </div>
            <div>
              <span className="profile-overview-label">Customer profile</span>
              <h2>{user?.fullName}</h2>
              <p>{user?.email}</p>
            </div>
          </div>

          <div className="profile-overview-stats">
            <div className="profile-stat">
              <ShieldCheck size={18} aria-hidden="true" />
              <span>Role</span>
              <strong>{user?.role}</strong>
            </div>
            <div className="profile-stat">
              <Ruler size={18} aria-hidden="true" />
              <span>Fit data</span>
              <strong>{completedFitFields}/7</strong>
            </div>
            <div className="profile-stat">
              <Image size={18} aria-hidden="true" />
              <span>Try-on photos</span>
              <strong>{completedTryOnPhotos}/4</strong>
            </div>
            <div className="profile-stat">
              <MapPin size={18} aria-hidden="true" />
              <span>Address</span>
              <strong>{completedAddressFields}/5</strong>
            </div>
          </div>
        </aside>

        <section className="profile-grid">
          <article className="profile-card account-card">
          <div className="profile-card-header">
            <div>
              <span className="profile-section-kicker">Personal information</span>
              <h2>Account details</h2>
            </div>
            {renderSectionActions('account', 'account')}
          </div>

          {!editingAccount ? (
            <>
              {success ? (
                <div className="alert alert-success profile-alert" role="status">
                  {success}
                </div>
              ) : null}
              <dl className="detail-list">
                <div>
                  <dt>
                    <UserRound size={17} aria-hidden="true" />
                    Full name
                  </dt>
                  <dd>{user?.fullName}</dd>
                </div>
                <div>
                  <dt>
                    <UserRound size={17} aria-hidden="true" />
                    Gender
                  </dt>
                  <dd>{formatGender(user?.gender)}</dd>
                </div>
                <div>
                  <dt>
                    <Phone size={17} aria-hidden="true" />
                    Phone number
                  </dt>
                  <dd>{displayValue(user?.phoneNumber)}</dd>
                </div>
                <div>
                  <dt>
                    <Mail size={17} aria-hidden="true" />
                    Email
                  </dt>
                  <dd>{user?.email}</dd>
                </div>
                <div>
                  <dt>
                    <ShieldCheck size={17} aria-hidden="true" />
                    Role
                  </dt>
                  <dd>{user?.role}</dd>
                </div>
                <div>
                  <dt>
                    <CalendarDays size={17} aria-hidden="true" />
                    Date of birth
                  </dt>
                  <dd>{formatDate(user?.dateOfBirth)}</dd>
                </div>
              </dl>
            </>
          ) : (
            <form id="profile-edit-form" className="profile-form" onSubmit={handleSubmit}>
              {formError ? (
                <div className="alert alert-error" role="alert">
                  {formError}
                </div>
              ) : null}

              <div className="profile-photo-field">
                <div className={values.profilePhotoUrl ? 'avatar avatar-photo' : 'avatar'} aria-hidden="true">
                  {values.profilePhotoUrl ? (
                    <img src={values.profilePhotoUrl} alt="" />
                  ) : (
                    values.fullName?.charAt(0).toUpperCase() || 'U'
                  )}
                </div>
                <label className="secondary-button photo-upload-button" htmlFor="profile-photo">
                  <Camera size={17} aria-hidden="true" />
                  <span>Profile photo</span>
                </label>
                <input
                  id="profile-photo"
                  type="file"
                  accept="image/png,image/jpeg,image/webp"
                  onChange={updateProfilePhoto}
                />
                <div className="photo-validation-copy">
                  <span>{getImageValidationRequirement('profilePhotoUrl')}</span>
                  {photoErrors.profilePhotoUrl ? (
                    <p className="field-error" role="alert">
                      {photoErrors.profilePhotoUrl}
                    </p>
                  ) : null}
                </div>
              </div>

              <div className="profile-form-grid">
                <div className="field">
                  <label htmlFor="profile-full-name">Full name</label>
                  <input id="profile-full-name" name="fullName" value={values.fullName} onChange={updateValue} />
                </div>
                <div className="field">
                  <label htmlFor="profile-phone">Phone number</label>
                  <input id="profile-phone" name="phoneNumber" value={values.phoneNumber} onChange={updateValue} />
                </div>
                <div className="field">
                  <label htmlFor="profile-gender">Gender</label>
                  <select id="profile-gender" name="gender" value={values.gender} onChange={updateValue}>
                    <option value="male">Male</option>
                    <option value="female">Female</option>
                    <option value="other">Other</option>
                  </select>
                </div>
                <div className="field">
                  <label htmlFor="profile-date-of-birth">Date of birth</label>
                  <input
                    id="profile-date-of-birth"
                    name="dateOfBirth"
                    type="date"
                    value={values.dateOfBirth}
                    onChange={updateValue}
                  />
                </div>
              </div>

            </form>
          )}
          </article>

        <article className="profile-card delivery-address-card">
          <div className="profile-card-header">
            <div>
              <span className="profile-section-kicker">Shipping information</span>
              <h2>Delivery Address</h2>
            </div>
            {renderSectionActions('address', 'address')}
          </div>
          {!editingAddress ? (
            <dl className="detail-list address-detail-list">
              <div>
                <dt>
                  <MapPin size={17} aria-hidden="true" />
                  Country
                </dt>
                <dd>{displayValue(user?.deliveryCountry)}</dd>
              </div>
              <div>
                <dt>
                  <MapPin size={17} aria-hidden="true" />
                  City
                </dt>
                <dd>{displayValue(user?.deliveryCity)}</dd>
              </div>
              <div>
                <dt>
                  <MapPin size={17} aria-hidden="true" />
                  Street
                </dt>
                <dd>{displayValue(user?.deliveryStreet)}</dd>
              </div>
              <div>
                <dt>
                  <Building2 size={17} aria-hidden="true" />
                  Building
                </dt>
                <dd>{displayValue(user?.deliveryBuilding)}</dd>
              </div>
              <div>
                <dt>
                  <Phone size={17} aria-hidden="true" />
                  Phone number
                </dt>
                <dd>{displayValue(user?.deliveryPhoneNumber)}</dd>
              </div>
            </dl>
          ) : (
            <div className="profile-form address-form">
              <div className="profile-form-grid">
                <div className="field">
                  <label htmlFor="delivery-country">Country</label>
                  <input
                    id="delivery-country"
                    name="deliveryCountry"
                    value={values.deliveryCountry}
                    onChange={updateValue}
                  />
                </div>
                <div className="field">
                  <label htmlFor="delivery-city">City</label>
                  <input
                    id="delivery-city"
                    name="deliveryCity"
                    value={values.deliveryCity}
                    onChange={updateValue}
                  />
                </div>
                <div className="field">
                  <label htmlFor="delivery-street">Street</label>
                  <input
                    id="delivery-street"
                    name="deliveryStreet"
                    value={values.deliveryStreet}
                    onChange={updateValue}
                  />
                </div>
                <div className="field">
                  <label htmlFor="delivery-building">Building</label>
                  <input
                    id="delivery-building"
                    name="deliveryBuilding"
                    value={values.deliveryBuilding}
                    onChange={updateValue}
                  />
                </div>
                <div className="field">
                  <label htmlFor="delivery-phone-number">Phone number</label>
                  <input
                    id="delivery-phone-number"
                    name="deliveryPhoneNumber"
                    value={values.deliveryPhoneNumber}
                    onChange={updateValue}
                  />
                </div>
              </div>
            </div>
          )}
        </article>

        <article className="profile-card saved-outfits-card">
          <div className="profile-card-header">
            <h2>Saved Outfits</h2>
            <Heart size={20} aria-hidden="true" />
          </div>
          <p className="saved-outfits-count">0 saved outfits</p>
          <button className="secondary-button saved-outfits-button" type="button">
            View saved outfits
          </button>
        </article>

        <article className="profile-card order-history-card">
          <div className="profile-card-header">
            <h2>My Orders</h2>
            <Package size={20} aria-hidden="true" />
          </div>
          <p className="order-history-state">No orders yet</p>
          <button className="secondary-button order-history-button" type="button">
            View order history
          </button>
        </article>

        <article className="profile-card fit-card">
          <div className="profile-card-header">
            <div>
              <span className="profile-section-kicker">Sizing preferences</span>
              <h2>Fit profile</h2>
            </div>
            <div className="profile-card-header-actions">
              <span className="profile-card-count">{completedFitFields}/7 added</span>
              {renderSectionActions('fit', 'fit')}
            </div>
          </div>
          {!editingFit ? (
            <dl className="detail-list fit-detail-list">
              <div>
                <dt>
                  <Ruler size={17} aria-hidden="true" />
                  Height
                </dt>
                <dd>{displayValue(user?.heightCm, ' cm')}</dd>
              </div>
              <div>
                <dt>
                  <Scale size={17} aria-hidden="true" />
                  Weight
                </dt>
                <dd>{displayValue(user?.weightKg, ' kg')}</dd>
              </div>
              <div>
                <dt>
                  <Shirt size={17} aria-hidden="true" />
                  Preferred size
                </dt>
                <dd>{displayValue(user?.preferredSize)}</dd>
              </div>
              <div>
                <dt>
                  <UserRound size={17} aria-hidden="true" />
                  Body shape
                </dt>
                <dd>{displayValue(user?.bodyShape)}</dd>
              </div>
              <div>
                <dt>
                  <Footprints size={17} aria-hidden="true" />
                  Shoe size
                </dt>
                <dd>{displayValue(user?.shoeSize)}</dd>
              </div>
              <div>
                <dt>
                  <Shirt size={17} aria-hidden="true" />
                  Top size
                </dt>
                <dd>{displayValue(user?.topSize)}</dd>
              </div>
              <div>
                <dt>
                  <Ruler size={17} aria-hidden="true" />
                  Bottom size
                </dt>
                <dd>{displayValue(user?.bottomSize)}</dd>
              </div>
            </dl>
          ) : (
            <div className="profile-form fit-form">
              <div className="profile-form-grid">
                <div className="field">
                  <label htmlFor="profile-height">Height</label>
                  <input
                    id="profile-height"
                    name="heightCm"
                    type="number"
                    min="50"
                    max="260"
                    step="0.1"
                    value={values.heightCm}
                    onChange={updateValue}
                  />
                </div>
                <div className="field">
                  <label htmlFor="profile-weight">Weight</label>
                  <input
                    id="profile-weight"
                    name="weightKg"
                    type="number"
                    min="20"
                    max="300"
                    step="0.1"
                    value={values.weightKg}
                    onChange={updateValue}
                  />
                </div>
                <div className="field">
                  <label htmlFor="profile-preferred-size">Preferred size</label>
                  <input
                    id="profile-preferred-size"
                    name="preferredSize"
                    value={values.preferredSize}
                    onChange={updateValue}
                    placeholder="M"
                  />
                </div>
                <div className="field">
                  <label htmlFor="profile-body-shape">Body shape</label>
                  <input
                    id="profile-body-shape"
                    name="bodyShape"
                    value={values.bodyShape}
                    onChange={updateValue}
                    placeholder="Rectangle"
                  />
                </div>
                <div className="field">
                  <label htmlFor="profile-shoe-size">Shoe size</label>
                  <input
                    id="profile-shoe-size"
                    name="shoeSize"
                    value={values.shoeSize}
                    onChange={updateValue}
                    placeholder="42"
                  />
                </div>
                <div className="field">
                  <label htmlFor="profile-top-size">Top size</label>
                  <input
                    id="profile-top-size"
                    name="topSize"
                    value={values.topSize}
                    onChange={updateValue}
                    placeholder="M"
                  />
                </div>
                <div className="field">
                  <label htmlFor="profile-bottom-size">Bottom size</label>
                  <input
                    id="profile-bottom-size"
                    name="bottomSize"
                    value={values.bottomSize}
                    onChange={updateValue}
                    placeholder="32"
                  />
                </div>
              </div>
            </div>
          )}
        </article>

        <article className="profile-card tryon-card">
          <div className="profile-card-header">
            <div>
              <span className="profile-section-kicker">AI-ready images</span>
              <h2>Try-On Photos</h2>
              <p className="profile-section-description">Use clear, well-framed photos for more accurate virtual try-on results.</p>
            </div>
            <div className="profile-card-header-actions">
              <span className="profile-card-count">{completedTryOnPhotos}/4 added</span>
              {renderSectionActions('photos', 'photos')}
            </div>
          </div>
          <div className="tryon-photo-grid">
            {tryOnPhotos.map((photo) => {
              const value = editingPhotos ? values[photo.key] : user?.[photo.key];

              return (
                <div className="tryon-photo-item" key={photo.key}>
                  <div className={value ? 'tryon-photo-preview has-photo' : 'tryon-photo-preview'}>
                    {value ? <img src={value} alt="" /> : <Image size={28} aria-hidden="true" />}
                    {value ? (
                      <button
                        className="tryon-photo-preview-button"
                        type="button"
                        onClick={() => setViewedPhoto({ source: value, title: photo.title })}
                        aria-label={`View ${photo.title}`}
                      >
                        <Eye size={18} />
                        <span>View</span>
                      </button>
                    ) : null}
                  </div>
                  <div className="tryon-photo-copy">
                    <strong>{photo.title}</strong>
                    <span>{photo.description}</span>
                  </div>
                  <div className="tryon-photo-actions">
                    {value ? (
                      <div className="tryon-photo-quick-actions">
                        <button
                          className="secondary-button compact-button"
                          type="button"
                          onClick={() => setViewedPhoto({ source: value, title: photo.title })}
                        >
                          <Eye size={16} aria-hidden="true" />
                          <span>View</span>
                        </button>
                        <button
                          className="secondary-button compact-button"
                          type="button"
                          onClick={() => adjustExistingPhoto(photo.key, value)}
                          disabled={editing && !editingPhotos}
                          title={editing && !editingPhotos ? 'Save or cancel the section you are currently editing first.' : undefined}
                        >
                          <SlidersHorizontal size={16} aria-hidden="true" />
                          <span>Adjust</span>
                        </button>
                      </div>
                    ) : null}
                    {editingPhotos ? (
                      <>
                      <label className="secondary-button photo-upload-button" htmlFor={photo.key}>
                        <Upload size={16} aria-hidden="true" />
                        <span>{value ? 'Change photo' : 'Upload photo'}</span>
                      </label>
                      <input
                        id={photo.key}
                        type="file"
                        accept="image/png,image/jpeg,image/webp"
                        onChange={(event) => updateTryOnPhoto(photo.key, event)}
                      />
                      <button
                        className="ghost-button compact-button"
                        type="button"
                        onClick={() => removeTryOnPhoto(photo.key)}
                        disabled={!value}
                      >
                        <Trash2 size={16} aria-hidden="true" />
                        <span>Remove photo</span>
                      </button>
                      </>
                    ) : null}
                      {photoErrors[photo.key] ? (
                        <p className="field-error photo-validation-error" role="alert">
                          {photoErrors[photo.key]}
                        </p>
                      ) : null}
                  </div>
                </div>
              );
            })}
          </div>
        </article>
      </section>
      {pendingPhoto ? (
        <ImageAdjuster
          file={pendingPhoto.file}
          fieldName={pendingPhoto.fieldName}
          label={photoLabels[pendingPhoto.fieldName] || 'photo'}
          onApply={applyAdjustedPhoto}
          onCancel={() => setPendingPhoto(null)}
        />
      ) : null}
      {viewedPhoto ? (
        <div className="image-viewer-backdrop" role="presentation" onMouseDown={(event) => {
          if (event.target === event.currentTarget) setViewedPhoto(null);
        }}>
          <section className="image-viewer" role="dialog" aria-modal="true" aria-labelledby="image-viewer-title">
            <div className="image-viewer-header">
              <div>
                <span className="profile-section-kicker">Image preview</span>
                <h2 id="image-viewer-title">{viewedPhoto.title}</h2>
              </div>
              <button className="icon-button" type="button" onClick={() => setViewedPhoto(null)} aria-label="Close image preview">
                <X size={20} />
              </button>
            </div>
            <div className="image-viewer-stage">
              <img src={viewedPhoto.source} alt={viewedPhoto.title} />
            </div>
          </section>
        </div>
      ) : null}
      </section>
    </main>
  );
}
