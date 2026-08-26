using Microsoft.EntityFrameworkCore;
using VirtualTryOn.Api.Constants;
using VirtualTryOn.Api.Data;
using VirtualTryOn.Api.DTOs.Admin;
using VirtualTryOn.Api.Models;

namespace VirtualTryOn.Api.Services;

public class PromotionService(
    AppDbContext dbContext,
    IEmailSender emailSender)
{
    public async Task<IReadOnlyList<PromotionResponse>> GetAllAsync(CancellationToken cancellationToken)
    {
        var items = await dbContext.Promotions.AsNoTracking()
            .Include(item => item.Products)
            .Include(item => item.EmailDeliveries)
            .OrderByDescending(item => item.CreatedAt)
            .ToListAsync(cancellationToken);
        return items.Select(ToResponse).ToList();
    }

    public async Task<PromotionResponse?> CreateAsync(
        CreatePromotionRequest request,
        CancellationToken cancellationToken)
    {
        var productIds = request.ProductIds.Distinct().ToList();
        var existingCount = await dbContext.Products.CountAsync(
            product => productIds.Contains(product.Id), cancellationToken);
        if (existingCount != productIds.Count) return null;

        var promotion = new Promotion
        {
            Name = request.Name.Trim(),
            DiscountPercentage = request.DiscountPercentage,
            StartsAt = request.StartsAt,
            EndsAt = request.EndsAt,
            IsActive = true,
            Products = productIds.Select(id => new PromotionProduct { ProductId = id }).ToList()
        };
        dbContext.Promotions.Add(promotion);
        await dbContext.SaveChangesAsync(cancellationToken);

        if (request.SendEmail)
            await SendCampaignAsync(promotion.Id, cancellationToken);

        return await GetAsync(promotion.Id, cancellationToken);
    }

    public async Task<PromotionResponse?> SendCampaignAsync(
        Guid promotionId,
        CancellationToken cancellationToken)
    {
        var promotion = await dbContext.Promotions
            .Include(item => item.EmailDeliveries)
            .SingleOrDefaultAsync(item => item.Id == promotionId, cancellationToken);
        if (promotion is null) return null;

        var alreadyAttempted = promotion.EmailDeliveries.Select(item => item.UserId).ToHashSet();
        var customers = await dbContext.Users.AsNoTracking()
            .Include(user => user.Profile)
            .Where(user => user.Role == UserRoles.Customer &&
                           user.IsEmailVerified &&
                           !alreadyAttempted.Contains(user.Id))
            .ToListAsync(cancellationToken);

        foreach (var customer in customers)
        {
            var delivery = new PromotionEmailDelivery
            {
                PromotionId = promotion.Id,
                UserId = customer.Id,
                Email = customer.Email
            };
            try
            {
                await emailSender.SendPromotionEmailAsync(
                    customer.Email,
                    customer.Profile?.FullName ?? customer.Email,
                    promotion.Name,
                    promotion.DiscountPercentage,
                    promotion.EndsAt,
                    cancellationToken);
                delivery.Succeeded = true;
            }
            catch (Exception exception)
            {
                delivery.ErrorMessage = exception.Message.Length > 1000
                    ? exception.Message[..1000]
                    : exception.Message;
            }
            dbContext.PromotionEmailDeliveries.Add(delivery);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        promotion.EmailSent = true;
        await dbContext.SaveChangesAsync(cancellationToken);
        return await GetAsync(promotion.Id, cancellationToken);
    }

    public async Task<PromotionResponse?> DeactivateAsync(Guid id, CancellationToken cancellationToken)
    {
        var promotion = await dbContext.Promotions.SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (promotion is null) return null;
        promotion.IsActive = false;
        await dbContext.SaveChangesAsync(cancellationToken);
        return await GetAsync(id, cancellationToken);
    }

    private async Task<PromotionResponse?> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        var promotion = await dbContext.Promotions.AsNoTracking()
            .Include(item => item.Products)
            .Include(item => item.EmailDeliveries)
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        return promotion is null ? null : ToResponse(promotion);
    }

    private static PromotionResponse ToResponse(Promotion item) => new(
        item.Id, item.Name, item.DiscountPercentage, item.StartsAt, item.EndsAt,
        item.IsActive, item.EmailSent, item.Products.Count,
        item.EmailDeliveries.Count(delivery => delivery.Succeeded),
        item.EmailDeliveries.Count(delivery => !delivery.Succeeded),
        item.Products.Select(product => product.ProductId).ToList());
}
