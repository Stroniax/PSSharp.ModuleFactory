using System.Management.Automation.Language;

namespace PSSharp.ModuleFactory
{
    internal static class AstExtensions
    {
        public static T? Find<T>(this Ast ast, bool searchNestedScriptBlocks)
            where T : Ast
        {
            var result = ast.Find(static ast => ast is T, searchNestedScriptBlocks);
            return (T)result;
        }
        public static IEnumerable<T> FindAll<T>(this Ast ast, bool searchNestedScriptBlocks)
            where T : Ast
        {
            foreach (var result in ast.FindAll(static ast => ast is T, searchNestedScriptBlocks))
            {
                yield return (T)result;
            }
        }
    }
    internal static class PathIntrinsicsExtensions
    {
        /// <summary>
        /// Sets the path of <paramref name="pathIntrinsics"/> to <paramref name="path"/>. Disposing of the
        /// returned value will revert to the original path.
        /// </summary>
        /// <param name="pathIntrinsics">Path intrinsics that will be used to visit the path.</param>
        /// <param name="path">The resolved path to visit.</param>
        /// <returns>An instance that may be disposed to return to the initial path.</returns>
        public static IDisposable VisitPath(this PathIntrinsics pathIntrinsics, string path)
        {
            if (pathIntrinsics.CurrentLocation.Path.Equals(path, StringComparison.OrdinalIgnoreCase))
                return default(Undisposable);
            return PathVisitor.VisitPath(pathIntrinsics, path);
        }
        private readonly struct Undisposable : IDisposable
        {
            public void Dispose() { }
        }
        private sealed class PathVisitor : IDisposable
        {
            public static IDisposable VisitPath(PathIntrinsics pathIntrinsics, string path)
            {
                return new PathVisitor(pathIntrinsics, path);
            }
            private readonly PathIntrinsics _pathIntrinsics;
            private bool _isReverted;
            private readonly string _stack;
            private PathVisitor(PathIntrinsics pathIntrinsics, string path)
            {
                _pathIntrinsics = pathIntrinsics;
                _stack = Guid.NewGuid().ToString();
                pathIntrinsics.PushCurrentLocation(_stack);
                try
                {
                    pathIntrinsics.SetLocation(path);
                }
                catch
                {
                    pathIntrinsics.PopLocation(_stack);
                    throw;
                }
            }
            public void Dispose()
            {
                if (!_isReverted)
                {
                    _isReverted = true;
                    _pathIntrinsics.PopLocation(_stack);
                }
            }
        }

    }
}
