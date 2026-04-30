
namespace Common.Helpers
{
    public class CommentsHelper
    {
        public static int CountComments(List<int> kids)
        {
            return kids == null ? 0 : kids.Count;
        }
    }
}
