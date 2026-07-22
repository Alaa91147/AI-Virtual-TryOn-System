import { useEffect, useState } from 'react';
import {
  MessageSquare,
  Pencil,
  Star,
  Trash2,
} from 'lucide-react';

import { reviewService } from '../../services/reviewService.js';
import { getErrorMessage } from '../../services/authService.js';

const emptyReviews = {
  averageRating: 0,
  reviewCount: 0,
  currentUserReview: null,
  reviews: [],
};

export default function ProductReviews({
  productId,
  token,
  onSummaryChange,
}) {
  const [data, setData] = useState(emptyReviews);
  const [rating, setRating] = useState(0);
  const [hoveredRating, setHoveredRating] =
    useState(0);
  const [comment, setComment] = useState('');
  const [loading, setLoading] = useState(true);
  const [submitting, setSubmitting] =
    useState(false);
  const [deleting, setDeleting] = useState(false);
  const [error, setError] = useState('');
  const [message, setMessage] = useState('');

  useEffect(() => {
    let cancelled = false;

    async function loadReviews() {
      try {
        setLoading(true);
        setError('');

        const response = await reviewService.get(
          token,
          productId,
        );

        if (!cancelled) {
          setData(response);

          if (response.currentUserReview) {
            setRating(
              response.currentUserReview.rating,
            );
            setComment(
              response.currentUserReview.comment,
            );
          }
        }
      } catch (requestError) {
        if (!cancelled) {
          setError(
            getErrorMessage(
              requestError,
              'Could not load reviews.',
            ),
          );
        }
      } finally {
        if (!cancelled) {
          setLoading(false);
        }
      }
    }

    loadReviews();

    return () => {
      cancelled = true;
    };
  }, [productId, token]);

  function applyResponse(response) {
    setData(response);

    if (response.currentUserReview) {
      setRating(
        response.currentUserReview.rating,
      );
      setComment(
        response.currentUserReview.comment,
      );
    } else {
      setRating(0);
      setComment('');
    }

    onSummaryChange?.(response);
  }

  async function submitReview(event) {
    event.preventDefault();

    if (rating < 1) {
      setError('Please select a star rating.');
      return;
    }

    if (comment.trim().length < 3) {
      setError(
        'Your comment must contain at least 3 characters.',
      );
      return;
    }

    try {
      setSubmitting(true);
      setError('');
      setMessage('');

      const payload = {
        rating,
        comment: comment.trim(),
      };

      const response = data.currentUserReview
        ? await reviewService.update(
            token,
            productId,
            data.currentUserReview.id,
            payload,
          )
        : await reviewService.create(
            token,
            productId,
            payload,
          );

      applyResponse(response);

      setMessage(
        data.currentUserReview
          ? 'Your review was updated.'
          : 'Thank you for sharing your review.',
      );
    } catch (requestError) {
      setError(
        getErrorMessage(
          requestError,
          'Could not save your review.',
        ),
      );
    } finally {
      setSubmitting(false);
    }
  }

  async function deleteReview() {
    if (
      !data.currentUserReview ||
      !window.confirm(
        'Delete your review permanently?',
      )
    ) {
      return;
    }

    try {
      setDeleting(true);
      setError('');
      setMessage('');

      const response = await reviewService.remove(
        token,
        productId,
        data.currentUserReview.id,
      );

      applyResponse(response);
      setMessage('Your review was deleted.');
    } catch (requestError) {
      setError(
        getErrorMessage(
          requestError,
          'Could not delete your review.',
        ),
      );
    } finally {
      setDeleting(false);
    }
  }

  function editReview(review) {
    setRating(review.rating);
    setComment(review.comment);

    document
      .getElementById('your-review-form')
      ?.scrollIntoView({
        behavior: 'smooth',
        block: 'center',
      });
  }

  function formatDate(value) {
    return new Intl.DateTimeFormat(undefined, {
      year: 'numeric',
      month: 'short',
      day: 'numeric',
    }).format(new Date(value));
  }

  if (loading) {
    return (
      <section className="product-reviews product-reviews-loading">
        <span className="spinner" />
        <p>Loading customer reviews...</p>
      </section>
    );
  }

  return (
    <section
      className="product-reviews"
      id="product-reviews"
    >
      <header className="reviews-header">
        <div>
          <p className="reviews-eyebrow">
            Customer experience
          </p>

          <h2>Ratings & Reviews</h2>
        </div>

        <div className="reviews-summary">
          <strong>
            {Number(data.averageRating).toFixed(1)}
          </strong>

          <div>
            <span>
              {[1, 2, 3, 4, 5].map((star) => (
                <Star
                  key={star}
                  size={17}
                  fill={
                    star <=
                    Math.round(data.averageRating)
                      ? 'currentColor'
                      : 'none'
                  }
                />
              ))}
            </span>

            <small>
              Based on {data.reviewCount}{' '}
              {data.reviewCount === 1
                ? 'review'
                : 'reviews'}
            </small>
          </div>
        </div>
      </header>

      <div className="reviews-layout">
        <form
          className="review-form"
          id="your-review-form"
          onSubmit={submitReview}
        >
          <div className="review-form-heading">
            <span>
              <MessageSquare size={20} />
            </span>

            <div>
              <p className="reviews-eyebrow">
                Your opinion matters
              </p>

              <h3>
                {data.currentUserReview
                  ? 'Edit your review'
                  : 'Write a review'}
              </h3>
            </div>
          </div>

          <fieldset className="review-rating-field">
            <legend>Your rating</legend>

            <div
              onMouseLeave={() =>
                setHoveredRating(0)
              }
            >
              {[1, 2, 3, 4, 5].map((star) => (
                <button
                  key={star}
                  type="button"
                  onMouseEnter={() =>
                    setHoveredRating(star)
                  }
                  onFocus={() =>
                    setHoveredRating(star)
                  }
                  onBlur={() =>
                    setHoveredRating(0)
                  }
                  onClick={() =>
                    setRating(star)
                  }
                  aria-label={`${star} stars`}
                >
                  <Star
                    size={27}
                    fill={
                      star <=
                      (hoveredRating || rating)
                        ? 'currentColor'
                        : 'none'
                    }
                  />
                </button>
              ))}
            </div>
          </fieldset>

          <label className="review-comment-field">
            <span>Your comment</span>

            <textarea
              value={comment}
              maxLength={2000}
              rows={6}
              placeholder="Tell others about the fit, quality, and style..."
              onChange={(event) =>
                setComment(event.target.value)
              }
            />

            <small>
              {comment.length}/2000 characters
            </small>
          </label>

          {error ? (
            <div
              className="alert alert-error"
              role="alert"
            >
              {error}
            </div>
          ) : null}

          {message ? (
            <div
              className="alert alert-success"
              role="status"
            >
              {message}
            </div>
          ) : null}

          <div className="review-form-actions">
            <button
              className="review-submit"
              type="submit"
              disabled={submitting}
            >
              {submitting
                ? 'Saving...'
                : data.currentUserReview
                  ? 'Update Review'
                  : 'Post Review'}
            </button>

            {data.currentUserReview ? (
              <button
                className="review-delete-own"
                type="button"
                disabled={deleting}
                onClick={deleteReview}
              >
                <Trash2 size={15} />
                {deleting ? 'Deleting...' : 'Delete'}
              </button>
            ) : null}
          </div>
        </form>

        <div className="reviews-list">
          <div className="reviews-list-heading">
            <h3>Customer comments</h3>
            <span>{data.reviewCount}</span>
          </div>

          {data.reviews.length ? (
            data.reviews.map((review) => (
              <article
                className={
                  review.isOwnReview
                    ? 'review-card is-own-review'
                    : 'review-card'
                }
                key={review.id}
              >
                <div className="review-card-top">
                  <div className="review-author">
                    <span>
                      {review.userName
                        .charAt(0)
                        .toUpperCase()}
                    </span>

                    <div>
                      <strong>
                        {review.userName}
                      </strong>

                      <small>
                        {formatDate(
                          review.updatedAt ||
                            review.createdAt,
                        )}

                        {review.updatedAt
                          ? ' · Edited'
                          : ''}
                      </small>
                    </div>
                  </div>

                  {review.isOwnReview ? (
                    <button
                      type="button"
                      onClick={() =>
                        editReview(review)
                      }
                    >
                      <Pencil size={14} />
                      Edit
                    </button>
                  ) : null}
                </div>

                <div
                  className="review-card-stars"
                  aria-label={`${review.rating} out of 5 stars`}
                >
                  {[1, 2, 3, 4, 5].map((star) => (
                    <Star
                      key={star}
                      size={15}
                      fill={
                        star <= review.rating
                          ? 'currentColor'
                          : 'none'
                      }
                    />
                  ))}
                </div>

                <p>{review.comment}</p>
              </article>
            ))
          ) : (
            <div className="reviews-empty">
              <MessageSquare size={29} />

              <h3>No reviews yet</h3>

              <p>
                Be the first customer to share an
                opinion about this product.
              </p>
            </div>
          )}
        </div>
      </div>
    </section>
  );
}