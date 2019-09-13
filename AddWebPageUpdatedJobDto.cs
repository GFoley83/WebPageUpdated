namespace WebPageUpdated
{
    public class AddWebPageUpdatedJobDto
    {
        public string Email { get; set; }
        public bool WatchIndefinitely { get; set; }
        public string WebPageUrl { get; set; }
        public string PathOfElementToWatch { get; set; }
        public string ElementMd5LastRun { get; set; }
    }
}
