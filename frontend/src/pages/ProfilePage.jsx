import {
  CalendarDays,
  Camera,
  Edit3,
  Footprints,
  Image,
  LogOut,
  Mail,
  Phone,
  Ruler,
  Save,
  Scale,
  ShieldCheck,
  Shirt,
  Trash2,
  Upload,
  UserRound,
  X,
} from 'lucide-react';
import { useState } from 'react';
import { getErrorMessage } from '../services/authService.js';
import { useAuth } from '../context/AuthContext.jsx';

const emptyForm = {
  fullName: '',
  gender: '',
  dateOfBirth: '',
  phoneNumber: '',
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
    description: 'Upload full body photo upper and lower',
  },
  {
    key: 'upperBodyPhotoUrl',
    title: 'Upper body photo',
    description: 'Upload only upper body for t-shirts, shirts, and other tops',
  },
  {
    key: 'lowerBodyPhotoUrl',
    title: 'Lower body photo',
    description: 'Upload only lower body for pants and other bottoms',
  },
  {
    key: 'facePhotoUrl',
    title: 'Face photo',
    description: 'Upload face photo for cap and accessory try-on later',
  },
];

export default function ProfilePage() {
  const { user, logout, updateProfile } = useAuth();
  const [editing, setEditing] = useState(false);
  const [values, setValues] = useState(() => toFormValues(user));
  const [formError, setFormError] = useState('');
  const [success, setSuccess] = useState('');
  const [saving, setSaving] = useState(false);

  function beginEdit() {
    setValues(toFormValues(user));
    setFormError('');
    setSuccess('');
    setEditing(true);
  }

  function cancelEdit() {
    setValues(toFormValues(user));
    setFormError('');
    setEditing(false);
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

  function updatePhotoField(fieldName, file) {
    if (!file) {
      return;
    }

    const reader = new FileReader();
    reader.onload = () => {
      setValues((current) => ({
        ...current,
        [fieldName]: String(reader.result || ''),
      }));
    };
    reader.readAsDataURL(file);
  }

  function updateProfilePhoto(event) {
    const file = event.target.files?.[0];
    if (!file) {
      return;
    }

    updatePhotoField('profilePhotoUrl', file);
  }

  function updateTryOnPhoto(fieldName, event) {
    updatePhotoField(fieldName, event.target.files?.[0]);
  }

  function removeTryOnPhoto(fieldName) {
    setValues((current) => ({
      ...current,
      [fieldName]: '',
    }));
  }

  async function handleSubmit(event) {
    event.preventDefault();

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
      setEditing(false);
    } catch (error) {
      setFormError(getErrorMessage(error, 'Could not update profile.'));
    } finally {
      setSaving(false);
    }
  }

  const photoUrl = editing ? values.profilePhotoUrl : user?.profilePhotoUrl;

  return (
    <main className="profile-shell">
      <header className="profile-topbar">
        <div>
          <span className="auth-kicker">AI Virtual Try-On Shop</span>
          <h1>Profile</h1>
        </div>
        <button className="ghost-button" type="button" onClick={logout}>
          <LogOut size={18} aria-hidden="true" />
          <span>Logout</span>
        </button>
      </header>

      <section className="profile-grid">
        <article className="profile-summary">
          <div className={photoUrl ? 'avatar avatar-photo' : 'avatar'} aria-hidden="true">
            {photoUrl ? <img src={photoUrl} alt="" /> : user?.fullName?.charAt(0).toUpperCase() || 'U'}
          </div>
          <div>
            <h2>{user?.fullName}</h2>
            <p>{user?.email}</p>
          </div>
        </article>

        <article className="profile-card">
          <div className="profile-card-header">
            <h2>Account details</h2>
            {!editing ? (
              <button className="ghost-button compact-button" type="button" onClick={beginEdit}>
                <Edit3 size={17} aria-hidden="true" />
                <span>Edit Profile</span>
              </button>
            ) : null}
          </div>

          {!editing ? (
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
            <form className="profile-form" onSubmit={handleSubmit}>
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
                <input id="profile-photo" type="file" accept="image/*" onChange={updateProfilePhoto} />
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

              <div className="profile-actions">
                <button className="secondary-button" type="button" onClick={cancelEdit} disabled={saving}>
                  <X size={17} aria-hidden="true" />
                  <span>Cancel</span>
                </button>
                <button className="primary-button fit-save-button" type="submit" disabled={saving}>
                  <Save size={17} aria-hidden="true" />
                  <span>{saving ? 'Saving...' : 'Save Profile'}</span>
                </button>
              </div>
            </form>
          )}
        </article>

        <article className="profile-card fit-card">
          <h2>Fit profile</h2>
          {!editing ? (
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
          <h2>My Try-On Photo</h2>
          <div className="tryon-photo-grid">
            {tryOnPhotos.map((photo) => {
              const value = editing ? values[photo.key] : user?.[photo.key];

              return (
                <div className="tryon-photo-item" key={photo.key}>
                  <div className={value ? 'tryon-photo-preview has-photo' : 'tryon-photo-preview'}>
                    {value ? <img src={value} alt="" /> : <Image size={28} aria-hidden="true" />}
                  </div>
                  <div className="tryon-photo-copy">
                    <strong>{photo.title}</strong>
                    <span>{photo.description}</span>
                  </div>
                  {editing ? (
                    <div className="tryon-photo-actions">
                      <label className="secondary-button photo-upload-button" htmlFor={photo.key}>
                        <Upload size={16} aria-hidden="true" />
                        <span>{value ? 'Change photo' : 'Upload photo'}</span>
                      </label>
                      <input
                        id={photo.key}
                        type="file"
                        accept="image/*"
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
                    </div>
                  ) : null}
                </div>
              );
            })}
          </div>
        </article>
      </section>
    </main>
  );
}
