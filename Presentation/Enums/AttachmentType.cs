namespace Presentation.Models
{
    /// <summary>
    /// Types of attachments supported by the messaging system
    /// </summary>
    public enum AttachmentType
    {
        /// <summary>
        /// No attachment
        /// </summary>
        None = 0,

        /// <summary>
        /// Image file (JPG, PNG, GIF, etc.)
        /// </summary>
        Image = 1,

        /// <summary>
        /// Video file (MP4, AVI, MOV, etc.)
        /// </summary>
        Video = 2,

        /// <summary>
        /// Audio file (MP3, WAV, OGG, etc.)
        /// </summary>
        Audio = 3,

        /// <summary>
        /// Document file (PDF, DOC, XLS, etc.)
        /// </summary>
        Document = 4
    }
}
