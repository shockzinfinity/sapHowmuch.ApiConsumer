using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sapHowmuchConsumer
{
	public static class TaskExtensions
	{
		public static object TryResult<T>(this Task<T> task)
		{
			if (task != null)
			{
				try
				{
					return task.Result;
				}
				catch
				{
					// ignore
				}
			}

			return task;
		}
	}
}
