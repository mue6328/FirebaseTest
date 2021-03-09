using Android.Content;
using Android.Support.V4.App;
using Android.Views;
using Android.Widget;
using Java.Lang;
using Fragment = Android.Support.V4.App.Fragment;

namespace FirebaseTest11
{
    public class PagerAdapter : FragmentPagerAdapter
    {
        Context context;

        public const int VideoTabIndex = 0;
        public const int TextTabIndex = 1;

        public PagerAdapter(FragmentManager fm, Context context)
                : base(fm)
        {
            this.context = context;
        }

        public override Fragment GetItem(int position)
        {
            switch (position)
            {
                case VideoTabIndex:
                    return new VideoChatFragment();
                case TextTabIndex:
                    return new TextChatFragment();
                default:
                    return null;
            }
        }

        public override ICharSequence GetPageTitleFormatted(int position)
        {
            switch (position)
            {
                case VideoTabIndex:
                    return new Java.Lang.String("Video");
                case TextTabIndex:
                    return new Java.Lang.String("Text");
                default:
                    return new Java.Lang.String("Unknown");
            }
        }

        public override int Count
        {
            get { return 2; }
        }

        public View GetTabView(int position)
        {
            View v = LayoutInflater.From(context).Inflate(Resource.Layout.tab, null);
            TextView header = (TextView)v.FindViewById(Resource.Id.header);
            header.Text = GetPageTitle(position);

            //TextView badge = (TextView)v.FindViewById(Resource.Id.badge);
            //badge.Visibility = ViewStates.Invisible;

            return v;
        }
    }
}