using Microsoft.AspNetCore.Builder;
using Microsoft.Data.Sqlite;
using Shared.Models;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Backend.Api
{
    public static class OfferApi
    {
        public static void MapOfferApi(this WebApplication app)
        {
            app.MapPost("/api/offers", (Func<OfferModel, HttpContext, Task<IResult>>)AddOffer);
            app.MapGet("/api/offers/pending", (Func<HttpContext, Task<IResult>>)GetPendingOffers);
            app.MapPost("/api/offers/accept/{id}", (Func<int, HttpContext, Task<IResult>>)AcceptOffer);
            app.MapPost("/api/offers/decline/{id}", (Func<int, HttpContext, Task<IResult>>)DeclineOffer);
        }

        private static async Task<IResult> AddOffer(OfferModel offer, HttpContext context)
        {
            // TODO: Hae todellinen käyttäjän ID autentikoinnista
            int userId = 1; // Tilapäinen kiinteä käyttäjä ID

            offer.UserID = userId;
            offer.Status = "Pending";
            offer.SubmissionDate = DateTime.UtcNow;

            try
            {
                using var connection = new SqliteConnection("Data Source=UsedPhonesShop.db");
                await connection.OpenAsync();

                var command = connection.CreateCommand();
                command.CommandText = @"
                    INSERT INTO Offers (UserID, PhoneBrand, PhoneModel, OriginalPrice, PhoneAge, OverallCondition, BatteryLife, ScreenCondition, CameraCondition, Status, SubmissionDate)
                    VALUES (@UserID, @PhoneBrand, @PhoneModel, @OriginalPrice, @PhoneAge, @OverallCondition, @BatteryLife, @ScreenCondition, @CameraCondition, @Status, @SubmissionDate)";

                command.Parameters.AddWithValue("@UserID", offer.UserID);
                command.Parameters.AddWithValue("@PhoneBrand", offer.PhoneBrand);
                command.Parameters.AddWithValue("@PhoneModel", offer.PhoneModel);
                command.Parameters.AddWithValue("@OriginalPrice", offer.OriginalPrice);
                command.Parameters.AddWithValue("@PhoneAge", offer.PhoneAge);
                command.Parameters.AddWithValue("@OverallCondition", offer.OverallCondition);
                command.Parameters.AddWithValue("@BatteryLife", offer.BatteryLife);
                command.Parameters.AddWithValue("@ScreenCondition", offer.ScreenCondition);
                command.Parameters.AddWithValue("@CameraCondition", offer.CameraCondition);
                command.Parameters.AddWithValue("@Status", offer.Status);
                command.Parameters.AddWithValue("@SubmissionDate", offer.SubmissionDate);

                await command.ExecuteNonQueryAsync();

                return Results.Ok(new { Message = "Arvio lähetetty onnistuneesti vahvistettavaksi." });
            }
            catch (Exception ex)
            {
                return Results.Problem($"Virhe arvioa lähetettäessä: {ex.Message}");
            }
        }

        private static async Task<IResult> GetPendingOffers(HttpContext context)
        {
            // TODO: Hae todellinen käyttäjän ID autentikoinnista
            int userId = 1; // Tilapäinen kiinteä käyttäjä ID

            try
            {
                using var connection = new SqliteConnection("Data Source=UsedPhonesShop.db");
                await connection.OpenAsync();

                var command = connection.CreateCommand();
                command.CommandText = @"
                    SELECT OfferID, UserID, PhoneBrand, PhoneModel, OriginalPrice, PhoneAge, OverallCondition, BatteryLife, ScreenCondition, CameraCondition, Status, SubmissionDate
                    FROM Offers
                    WHERE UserID = @UserID AND Status = 'Pending'";

                command.Parameters.AddWithValue("@UserID", userId);

                var offers = new List<OfferModel>();
                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    offers.Add(new OfferModel
                    {
                        OfferID = reader.GetInt32(0),
                        UserID = reader.GetInt32(1),
                        PhoneBrand = reader.GetString(2),
                        PhoneModel = reader.GetString(3),
                        OriginalPrice = reader.GetDecimal(4),
                        PhoneAge = reader.GetInt32(5),
                        OverallCondition = reader.GetInt32(6),
                        BatteryLife = reader.GetInt32(7),
                        ScreenCondition = reader.GetInt32(8),
                        CameraCondition = reader.GetInt32(9),
                        Status = reader.GetString(10),
                        SubmissionDate = reader.GetDateTime(11)
                    });
                }

                return Results.Ok(offers);
            }
            catch (Exception ex)
            {
                return Results.Problem($"Virhe arvioita haettaessa: {ex.Message}");
            }
        }

        private static async Task<IResult> AcceptOffer(int id, HttpContext context)
        {
            try
            {
                using var connection = new SqliteConnection("Data Source=UsedPhonesShop.db");
                await connection.OpenAsync();

                var command = connection.CreateCommand();
                command.CommandText = @"
                    UPDATE Offers
                    SET Status = 'Approved'
                    WHERE OfferID = @OfferID";
                command.Parameters.AddWithValue("@OfferID", id);

                var rowsAffected = await command.ExecuteNonQueryAsync();
                if (rowsAffected > 0)
                {
                    command.CommandText = "SELECT PhoneBrand, PhoneModel, OriginalPrice, PhoneAge, OverallCondition, BatteryLife, ScreenCondition, CameraCondition FROM Offers WHERE OfferID = @OfferID";
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            var phone = new PhoneModel
                            {
                                Brand = reader.GetString(0),
                                Model = reader.GetString(1),
                                Price = reader.GetDecimal(2),
                                Description = "Phone offered by user", // You can customize this
                                Condition = reader.GetString(3), // Assuming you have a condition mapping
                                StockQuantity = 1 // Set stock quantity as needed
                            };

                            using (var insertCommand = connection.CreateCommand())
                            {
                                insertCommand.CommandText = @"
                                    INSERT INTO Phones (Brand, Model, Price, Description, Condition, StockQuantity)
                                    VALUES (@Brand, @Model, @Price, @Description, @Condition, @StockQuantity)";
                                insertCommand.Parameters.AddWithValue("@Brand", phone.Brand);
                                insertCommand.Parameters.AddWithValue("@Model", phone.Model);
                                insertCommand.Parameters.AddWithValue("@Price", phone.Price);
                                insertCommand.Parameters.AddWithValue("@Description", phone.Description);
                                insertCommand.Parameters.AddWithValue("@Condition", phone.Condition);
                                insertCommand.Parameters.AddWithValue("@StockQuantity", phone.StockQuantity);

                                await insertCommand.ExecuteNonQueryAsync();
                            }
                        }
                    }

                    return Results.Ok(new { Message = "Tarjous hyväksytty onnistuneesti." });
                }
                else
                {
                    return Results.NotFound("Tarjousta ei löytynyt.");
                }
            }
            catch (Exception ex)
            {
                return Results.Problem($"Virhe tarjouksen hyväksymisessä: {ex.Message}");
            }
        }

        private static async Task<IResult> DeclineOffer(int id, HttpContext context)
        {
            try
            {
                using var connection = new SqliteConnection("Data Source=UsedPhonesShop.db");
                await connection.OpenAsync();

                var command = connection.CreateCommand();
                command.CommandText = @"
                    UPDATE Offers
                    SET Status = 'Rejected'
                    WHERE OfferID = @OfferID";
                command.Parameters.AddWithValue("@OfferID", id);

                var rowsAffected = await command.ExecuteNonQueryAsync();
                if (rowsAffected > 0)
                {
                    return Results.Ok(new { Message = "Tarjous hylätty onnistuneesti." });
                }
                else
                {
                    return Results.NotFound("Tarjousta ei löytynyt.");
                }
            }
            catch (Exception ex)
            {
                return Results.Problem($"Virhe tarjouksen hylkäämisessä: {ex.Message}");
            }
        }
    }
}
