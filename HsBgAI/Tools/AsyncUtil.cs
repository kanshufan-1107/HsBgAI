using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;

namespace HsBgAI.Tools
{
    public class AsyncUtil
    {
        /// <summary>
        /// 循环执行指定同步方法, 直到返回非 null 或超时
        /// </summary>
        /// <remarks>
        /// 这里刻意不使用 CancellationTokenSource + CancelAfterSlim：
        /// CancelAfterSlim 会往 PlayerLoop 注册一个定时器，到点后调用 cts.Cancel()。
        /// 而本方法常常在超时前就 return（拿到结果），此时 using 已经释放了 CTS，
        /// 定时器随后触发就会抛 ObjectDisposedException（The CancellationTokenSource has been disposed）。
        /// 改用 deadline 计时后不再持有任何 CTS，彻底避免该竞态。
        /// </remarks>
        /// <typeparam name="T">目标返回类型</typeparam>
        /// <param name="action">循环执行的同步方法, 返回目标类型的值</param>
        /// <param name="timeout">超时时间</param>
        /// <param name="checkInterval">检查间隔时间(默认 500 毫秒)</param>
        /// <returns>任务, 返回目标类型的结果. 如果超时, 返回默认值</returns>
        public static async UniTask<T> WaitForSync<T>(Func<T> action, TimeSpan timeout, TimeSpan? checkInterval = null)
        {
            var interval = checkInterval ?? TimeSpan.FromMilliseconds(500);
            var deadline = DateTime.UtcNow + timeout;

            while (DateTime.UtcNow < deadline)
            {
                var result = action();
                if (!EqualityComparer<T>.Default.Equals(result, default))
                    return result;

                var remain = deadline - DateTime.UtcNow;
                if (remain <= TimeSpan.Zero) break;

                await UniTask.Delay(remain < interval ? remain : interval);
            }

            return default;
        }

        /// <summary>
        /// 循环执行指定异步方法, 直到返回非 null 或超时
        /// </summary>
        /// <remarks>同 WaitForSync，使用 deadline 计时以避免 CTS 被提前释放引发的 ObjectDisposedException。</remarks>
        /// <typeparam name="T">目标返回类型</typeparam>
        /// <param name="action">循环执行的异步方法, 返回目标类型的值</param>
        /// <param name="timeout">超时时间</param>
        /// <param name="checkInterval">检查间隔时间(默认 500 毫秒)</param>
        /// <returns>任务, 返回目标类型的结果. 如果超时, 返回默认值</returns>
        public static async UniTask<T> WaitForAsync<T>(Func<UniTask<T>> action, TimeSpan timeout, TimeSpan? checkInterval = null)
        {
            var interval = checkInterval ?? TimeSpan.FromMilliseconds(500);
            var deadline = DateTime.UtcNow + timeout;

            while (DateTime.UtcNow < deadline)
            {
                var result = await action();
                if (!EqualityComparer<T>.Default.Equals(result, default))
                    return result;

                var remain = deadline - DateTime.UtcNow;
                if (remain <= TimeSpan.Zero) break;

                await UniTask.Delay(remain < interval ? remain : interval);
            }

            return default;
        }
    }
}
