#region

using SkiaSharp;

#endregion

namespace AGC_Management.Utils;

public sealed class ImageUtils
{
    public static SKBitmap CreateProgressBar(int width, int height, float percentage, string text)
    {
        var bmp = new SKBitmap(width, height);
        using var canvas = new SKCanvas(bmp);
        canvas.Clear(SKColors.White);

        var backgroundPaint = new SKPaint
        {
            Color = SKColors.Gray
        };
        canvas.DrawRect(0, 0, width, height, backgroundPaint);

        var progressPaint = new SKPaint
        {
            Color = SKColors.BlueViolet
        };
        canvas.DrawRect(0, 0, width * percentage, height, progressPaint);

        var textPaint = new SKPaint
        {
            Color = SKColors.White,
            FilterQuality = SKFilterQuality.High,
            TextSize = 18,
            TextAlign = SKTextAlign.Center,
            IsAntialias = true
        };
        var textBounds = new SKRect();
        using var typeface = SKTypeface.FromFamilyName("Verdana");
        textPaint.Typeface = typeface;
        textPaint.MeasureText(text, ref textBounds);
        canvas.DrawText(text, width / 2, (height + textBounds.Height) / 2, textPaint);

        return bmp;
    }

    public static async Task<SKData> GenerateRankCard(DiscordUser user, int level, int rank, float progression, int totalxp,
        int xpforthisleveltocomplete)
    {
        const int cardWidth = 934;
        const int cardHeight = 282;
        
        string font = "Verdana";

        try
        {
            font = BotConfig.GetConfig()["Leveling"]["RankCardFont"];
        }
        catch (Exception)
        {
        }
        
        
        using var bitmap = new SKBitmap(cardWidth, cardHeight);
        using var canvas = new SKCanvas(bitmap);
        
        var totalXP = Converter.FormatWithCommas(totalxp);
        var xptoCompleteCurrentLevel = Converter.FormatWithCommas(xpforthisleveltocomplete);
        var progress = progression / 100;
        
        using var httpclient = new HttpClient();
        httpclient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 " +
                                                        "(KHTML, like Gecko) Chrome/91.0.4472.124 Safari/537.36");
        
        var avatarurl = user.GetAvatarUrl(ImageFormat.Png);
        var response2 = await httpclient.GetAsync(avatarurl);
        if (!response2.IsSuccessStatusCode)
        {
            throw new InvalidDataException("Failed to download avatar image");
        }
        var avatarstream = await response2.Content.ReadAsByteArrayAsync();
        
        var avatar = SKBitmap.Decode(avatarstream);
        
        var avatarPaint = new SKPaint
        {
            IsAntialias = true,
            FilterQuality = SKFilterQuality.High,
        };
        
        var backgroundPaint = new SKPaint
        {
            IsAntialias = true,
        };
        
        int cardid = 0;
        string bgurl = "";
        var barcolor = SKColor.Empty;
        var db = CurrentApplication.ServiceProvider.GetRequiredService<NpgsqlDataSource>();
        await using var cmd = db.CreateCommand("SELECT bg_id FROM user_rankcardbackgrounds WHERE userid = @user_id");
        cmd.Parameters.AddWithValue("user_id", (long)user.Id);
        await using var reader = await cmd.ExecuteReaderAsync();    
        while (await reader.ReadAsync())
        {
            cardid = reader.GetInt32(0);
        }
        // check if the id is valid in db 
        await using var cmd2 = db.CreateCommand("SELECT bg_url, barcolor FROM rankcardbackgrounds WHERE bg_id = @id");
        cmd2.Parameters.AddWithValue("id", cardid);
        await using var reader2 = await cmd2.ExecuteReaderAsync();
        while (await reader2.ReadAsync())
        {
            bgurl = reader2.GetString(0);
            barcolor = SKColor.Parse(reader2.GetString(1));
        }
        if (reader2.HasRows == false)
        {
            cardid = 0;
        }

        try
        {
            cardid = int.Parse(BotConfig.GetConfig()["Leveling"]["DefaultRankCardBackgroundId"]);
        }
        catch (Exception)
        {

        }
        if (cardid > 0)
        {
            httpclient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 " +
                                                            "(KHTML, like Gecko) Chrome/91.0.4472.124 Safari/537.36");
            var response = await httpclient.GetAsync(bgurl);
            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidDataException("Failed to download background image");
            }
            var bgstream = await response.Content.ReadAsByteArrayAsync();
            using var bg_stream = new MemoryStream(bgstream);
            var backgroundBitmap = SKBitmap.Decode(bg_stream);
            canvas.DrawBitmap(backgroundBitmap, new SKRect(0, 0, cardWidth, cardHeight), backgroundPaint);
        }
        else
        {
            // black background without image
            canvas.Clear(SKColors.Black.WithAlpha(200));
        }
        var darkenPaint = new SKPaint
        {
            Color = new SKColor(0, 0, 0, 130),
            IsAntialias = true,
        };
        
        if (cardid == 0)
        {
            barcolor = SKColor.Parse("#9f00ff");
        }

        if (cardid != 0)
        {
            canvas.DrawRoundRect(new SKRect(15, 15, cardWidth - 15, cardHeight - 15), 20, 20, darkenPaint);
            
        }
        
        var avatarBitmap = avatar;
        var avatarRect = new SKRect(20, 20, 262, 262);
        using var mask = new SKPath();
        
        mask.AddRoundRect(avatarRect, 15, 15, SKPathDirection.Clockwise);
        
        canvas.Save();
        canvas.ClipPath(mask);
        canvas.DrawBitmap(avatarBitmap, avatarRect, avatarPaint);
        canvas.Restore();
        
        
        var namePaint = new SKPaint
        {
            Color = SKColors.White,
            IsAntialias = true,
            Typeface = SKTypeface.FromFamilyName(font, SKFontStyleWeight.Bold, SKFontStyleWidth.Normal, SKFontStyleSlant.Upright),
            TextAlign = SKTextAlign.Left,
            TextSize = 30
        };
        
        var nameBounds = new SKRect();
        namePaint.MeasureText(user.Username, ref nameBounds);
        canvas.DrawText(user.GetFormattedUserName(), 300, 70, namePaint);
        
        var rankLevelPaint = new SKPaint
        {
            Color = SKColors.White,
            TextSize = 25,
            IsAntialias = true,
            Typeface = SKTypeface.FromFamilyName("Verdana", SKFontStyleWeight.Bold, SKFontStyleWidth.Normal, SKFontStyleSlant.Upright),
        };
        
        canvas.DrawText($"Rang #{rank}   Level: {level}", 300, 110, rankLevelPaint);
        
        var progressBarBackgroundPaint = new SKPaint
        {
            Color = SKColors.LightGray.WithAlpha(140),
            Style = SKPaintStyle.Fill,
        };

        var progressBarBackgroundRect = new SKRect(300, 200, cardWidth - 30, 160);
        canvas.DrawRoundRect(new SKRoundRect(progressBarBackgroundRect, 10, 10), progressBarBackgroundPaint);
        
        var progressBarPaint = new SKPaint
        {
            Color = barcolor,
            Style = SKPaintStyle.Fill,
        };
        
        var progressBarRect = new SKRect(progressBarBackgroundRect.Left, progressBarBackgroundRect.Top, progressBarBackgroundRect.Left + (progressBarBackgroundRect.Width - 20) * progress, progressBarBackgroundRect.Bottom);
        canvas.DrawRoundRect(new SKRoundRect(progressBarRect, 10, 10), progressBarPaint);

               var xpText = $"{totalXP}/{xptoCompleteCurrentLevel} XP ({Math.Round(progress * 100, 2)}%)";
        var xpPaint = new SKPaint
        {
            TextSize = 25,
            TextAlign = SKTextAlign.Center,
            Color = SKColors.White,
            IsAntialias = true,
            Typeface = SKTypeface.FromFamilyName("Verdana")
        };

        float xpTextWidth = xpPaint.MeasureText(xpText);
        float xpTextX = progressBarBackgroundRect.Left + (progressBarBackgroundRect.Width / 2);
        float xpTextY = progressBarBackgroundRect.Top + (progressBarBackgroundRect.Height / 2) + (xpPaint.TextSize / 2);
        xpTextY -= 2;

        if (xpTextX + xpTextWidth / 2 > progressBarRect.Right)
        {
            canvas.Save();
            canvas.ClipRect(new SKRect(progressBarRect.Left, progressBarRect.Top, progressBarRect.Right, progressBarRect.Bottom));
            canvas.DrawText(xpText, xpTextX, xpTextY, xpPaint);
            canvas.Restore();

            xpPaint.Color = SKColors.White;

            canvas.Save();
            canvas.ClipRect(new SKRect(progressBarRect.Right, progressBarRect.Top, progressBarBackgroundRect.Right, progressBarBackgroundRect.Bottom));
            canvas.DrawText(xpText, xpTextX, xpTextY, xpPaint);
            canvas.Restore();
        }
        else
        {
            canvas.DrawText(xpText, xpTextX, xpTextY, xpPaint);
        }
        
        var totalXpPaint = new SKPaint
        {
            TextSize = 20,
            TextAlign = SKTextAlign.Center,
            Color = SKColors.White,
            IsAntialias = true,
            Typeface = SKTypeface.FromFamilyName("Verdana")
        };

        string totalXpText = $"Gesamt XP: {totalXP}";
        float totalXpTextWidth = totalXpPaint.MeasureText(totalXpText);
        float totalXpTextX = progressBarBackgroundRect.Left + 95;
        float totalXpTextY = progressBarBackgroundRect.Bottom + totalXpPaint.TextSize + 50; // Erhöhen Sie diesen Wert, um den Text weiter nach unten zu verschieben

        canvas.DrawText(totalXpText, totalXpTextX, totalXpTextY, totalXpPaint);

        using var image = SKImage.FromBitmap(bitmap);
        var data = image.Encode(SKEncodedImageFormat.Png, 100);
        return data;
        
        
    }
}