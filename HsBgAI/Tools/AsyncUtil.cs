using Cysharp.Threading.Tasks;

namespace HsBgAI.Tools
{
    public class AsyncUtil
    {
        /// <summary>
        /// 循环执行指定同步方法, 直到返回非 null 或超时
        /// </summary>
        /// <typeparam name="T">目标返回类型</typeparam>
        /// <param name="action">循环执行的同步方法, 返回目标类型的值</param>
        /// <param name="timeout">超时时间</param>
        /// <param name="checkInterval">检查间隔时间(默认 500 毫秒)</param>
        /// <returns>任务, 返回目标类型的结果. 如果超时, 返回默认值</returns>
        public static async UniTask<T> WaitForSync<T>(Func<T> action, TimeSpan timeout, TimeSpan? checkInterval = null)
        {
            var interval = checkInterval ?? TimeSpan.FromMilliseconds(500);

            // create a cancellation token for the timeout of the UniTask
            var timeoutTS = new CancellationTokenSource();
            timeoutTS.CancelAfterSlim(timeout);
            while (!timeoutTS.IsCancellationRequested)
            {
                // run sync action
                var result = action();

                // check if the result is not the default value of the type, if so, return the result
                if (!EqualityComparer<T>.Default.Equals(result, default))
                {
                    // cancel the timeout task, release resources
                    timeoutTS.Cancel();
                    return result;
                }
                await UniTask.Delay(interval, cancellationToken: timeoutTS.Token);
            }
            return default;
        }

        /// <summary>
        /// 循环执行指定异步方法, 直到返回非 null 或超时
        /// </summary>
        /// <typeparam name="T">目标返回类型</typeparam>
        /// <param name="action">循环执行的异步方法, 返回目标类型的值</param>
        /// <param name="timeout">超时时间</param>
        /// <param name="checkInterval">检查间隔时间(默认 500 毫秒)</param>
        /// <returns>任务, 返回目标类型的结果. 如果超时, 返回默认值</returns>
        public static async UniTask<T> WaitForAsync<T>(Func<UniTask<T>> action, TimeSpan timeout, TimeSpan? checkInterval = null)
        {
            var interval = checkInterval ?? TimeSpan.FromMilliseconds(500);

            // create a cancellation token for the timeout of the UniTask
            var timeoutTS = new CancellationTokenSource();
            timeoutTS.CancelAfterSlim(timeout);
            while (!timeoutTS.IsCancellationRequested)
            {
                // run async action
                var result = await action();

                // check if the result is not the default value of the type, if so, return the result
                if (!EqualityComparer<T>.Default.Equals(result, default))
                {
                    // cancel the timeout task, release resources
                    timeoutTS.Cancel();
                    return result;
                }
                await UniTask.Delay(interval, cancellationToken: timeoutTS.Token);
            }
            return default;
        }
    }
}