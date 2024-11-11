namespace Shared.Models
{
    public class OfferModel
    {
        public int OfferID { get; set; }
        public int UserID { get; set; }
        public string PhoneBrand { get; set; }
        public string PhoneModel { get; set; }
        public decimal OriginalPrice { get; set; }
        public int PhoneAge { get; set; }
        public int OverallCondition { get; set; }
        public int BatteryLife { get; set; }
        public int ScreenCondition { get; set; }
        public int CameraCondition { get; set; }
        public string Status { get; set; } // Esimerkiksi "Pending", "Approved", "Rejected"
        public DateTime SubmissionDate { get; set; }
    }
}
