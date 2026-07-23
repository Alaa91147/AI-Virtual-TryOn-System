import {
  useEffect,
  useRef,
  useState,
} from 'react';
import { useNavigate } from 'react-router-dom';
import { Bell, X } from 'lucide-react';

export default function NotificationToast() {
  const navigate = useNavigate();

  const [toast, setToast] = useState(null);
  const timeoutRef = useRef(null);
  const audioContextRef = useRef(null);

  useEffect(() => {
    function unlockAudio() {
      const AudioContext =
        window.AudioContext ||
        window.webkitAudioContext;

      if (!AudioContext) {
        return;
      }

      if (!audioContextRef.current) {
        audioContextRef.current =
          new AudioContext();
      }

      if (
        audioContextRef.current.state ===
        'suspended'
      ) {
        audioContextRef.current.resume();
      }
    }

    document.addEventListener(
      'pointerdown',
      unlockAudio,
      { once: true },
    );

    return () => {
      document.removeEventListener(
        'pointerdown',
        unlockAudio,
      );
    };
  }, []);

  useEffect(() => {
    function playSound() {
      const context =
        audioContextRef.current;

      if (!context) {
        return;
      }

      const start = context.currentTime;

      [
        { frequency: 740, delay: 0 },
        { frequency: 980, delay: 0.13 },
      ].forEach(({ frequency, delay }) => {
        const oscillator =
          context.createOscillator();

        const gain = context.createGain();

        oscillator.type = 'sine';
        oscillator.frequency.value =
          frequency;

        gain.gain.setValueAtTime(
          0,
          start + delay,
        );

        gain.gain.linearRampToValueAtTime(
          0.09,
          start + delay + 0.02,
        );

        gain.gain.exponentialRampToValueAtTime(
          0.001,
          start + delay + 0.22,
        );

        oscillator.connect(gain);
        gain.connect(context.destination);

        oscillator.start(start + delay);
        oscillator.stop(
          start + delay + 0.24,
        );
      });
    }

    function showNotification(event) {
      const detail = event.detail || {};

      setToast({
        title:
          detail.title || 'New notification',
        message:
          detail.message ||
          'Your account has a new update.',
        link: detail.link || null,
      });

      playSound();

      window.clearTimeout(
        timeoutRef.current,
      );

      timeoutRef.current =
        window.setTimeout(() => {
          setToast(null);
        }, 4000);
    }

    window.addEventListener(
      'app-notification',
      showNotification,
    );

    return () => {
      window.removeEventListener(
        'app-notification',
        showNotification,
      );

      window.clearTimeout(
        timeoutRef.current,
      );
    };
  }, []);

  function closeToast(event) {
    event.stopPropagation();
    setToast(null);

    window.clearTimeout(
      timeoutRef.current,
    );
  }

  function openToast() {
    if (toast?.link) {
      navigate(toast.link);
    }

    setToast(null);
  }

  if (!toast) {
    return null;
  }

  return (
    <aside
      className="app-notification-toast"
      role="status"
      onClick={openToast}
    >
      <span className="app-toast-icon">
        <Bell size={19} />
      </span>

      <span className="app-toast-copy">
        <strong>{toast.title}</strong>
        <span>{toast.message}</span>
      </span>

      <button
        type="button"
        onClick={closeToast}
        aria-label="Close notification"
      >
        <X size={16} />
      </button>
    </aside>
  );
}