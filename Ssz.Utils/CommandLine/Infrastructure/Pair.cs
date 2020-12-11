namespace Ssz.Utils.CommandLine.Infrastructure
{
    internal sealed class Pair<TLeft, TRight>
        where TLeft : class
        where TRight : class
    {
        #region construction and destruction

        public Pair(TLeft left, TRight right)
        {
            _left = left;
            _right = right;
        }

        #endregion

        #region public functions

        public override int GetHashCode()
        {
            int leftHash = _left == null ? 0 : _left.GetHashCode();
            int rightHash = _right == null ? 0 : _right.GetHashCode();

            return leftHash ^ rightHash;
        }

        public override bool Equals(object? obj)
        {
            var other = obj as Pair<TLeft, TRight>;

            if (other == null)
            {
                return false;
            }

            return Equals(_left, other._left) && Equals(_right, other._right);
        }

        public TLeft Left
        {
            get { return _left; }
        }

        public TRight Right
        {
            get { return _right; }
        }

        #endregion

        #region private fields

        private readonly TLeft _left;
        private readonly TRight _right;

        #endregion
    }
}