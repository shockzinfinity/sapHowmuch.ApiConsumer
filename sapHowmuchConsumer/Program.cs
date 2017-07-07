using Microsoft.Owin.Security.DataHandler.Encoder;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Linq;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Thinktecture.IdentityModel.Tokens;
using Microsoft.IdentityModel.Tokens;
using System.Security.Cryptography;
using System.IdentityModel.Claims;
using System.Text;

namespace sapHowmuchConsumer
{
	internal class Program
	{
		private const string _apiServer = "sapdev1.semubot.com:9091";
		//private const string _apiServer = "localhost:32926";
		private static string _userName = "emaxit";
		private static string _password = "2max!t63@!4";
		private static string _clientId = "dad3b434-2ec5-4976-a34b-e3ed3f1eae77"; // NOTE: must be stored securely
		private static string _clientSecret = "KERKZo_TiBQcj6zxI3eZ-xU7dHsJaWo18ThGarEqSZk"; // NOTE: must be stored securely
		private static TokenData _tokenResult;

		private static void Main(string[] args)
		{
			// api base address
			var baseUri = new Uri($"http://{_apiServer}");

			try
			{
				Console.WriteLine("------------- GET TOKEN PROCEDURE -------------");
				ExecuteOperation(() => GetToken(baseUri).Wait());

				Console.WriteLine($"AccessToken: {_tokenResult.AccessToken}{Environment.NewLine}RefreshTokenId: {_tokenResult.RefreshTokenId}");

				Console.WriteLine();
				Console.WriteLine();
				Console.WriteLine("------------- VALIDATE TOKEN PROCEDURE -------------");
				Console.WriteLine(IsValid());

				Console.WriteLine();
				Console.WriteLine();
				Console.WriteLine("------------- REFRESH TOKEN PROCEDURE -------------");

				if (IsValid())
				{
					ExecuteOperation(() => RefreshToken(baseUri).Wait());
					Console.WriteLine($"AccessToken: {_tokenResult.AccessToken}{Environment.NewLine}RefreshTokenId: {_tokenResult.RefreshTokenId}");
				}

				Console.WriteLine();
				Console.WriteLine();
				Console.WriteLine("------------- CONSUME WEBAPI (SELECT) -------------");
				ExecuteOperation(() => SelectJournalVouchersList(baseUri).Wait());

				Console.WriteLine();
				Console.WriteLine();
				Console.WriteLine("------------- CONSUME WEBAPI (CREATE JOURNAL VOUCHER) -------------");
				ExecuteOperation(() => CreateJournalVoucher(baseUri).Wait());
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex.Message);
			}

			Console.Write($"Press <Enter> to exit...");
			Console.ReadLine();
		}

		// refresh token
		// 토큰 검증
		// api consume (R)
		// 추후, (CUD) 추가 - api 에서도 작업 필요

		private static async Task GetToken(Uri baseUri)
		{
			using (HttpClient client = new HttpClient())
			{
				client.BaseAddress = baseUri;
				HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, string.Concat(baseUri, "auth/token"));

				client.DefaultRequestHeaders.Add("Accept", new MediaTypeHeaderValue("application/json").ToString());

				Dictionary<string, string> requestBody = new Dictionary<string, string>();
				requestBody.Add("grant_type", "password");
				requestBody.Add("username", _userName);
				requestBody.Add("password", _password);
				requestBody.Add("client_id", _clientId);

				FormUrlEncodedContent formDataContent = new FormUrlEncodedContent(requestBody);
				request.Content = formDataContent;

				await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead)
					.ContinueWith((response) =>
					{
						try
						{
							_tokenResult = ProcessToken(response);
						}
						catch (AggregateException exs)
						{
							Console.WriteLine($"Exceptions: {exs}");
						}
					});
			}
		}

		private static async Task RefreshToken(Uri baseUri)
		{
			using (HttpClient client = new HttpClient())
			{
				client.BaseAddress = baseUri;
				HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, string.Concat(baseUri, "auth/token"));

				client.DefaultRequestHeaders.Add("Accept", new MediaTypeHeaderValue("application/json").ToString());

				Dictionary<string, string> requestBody = new Dictionary<string, string>();
				requestBody.Add("grant_type", "refresh_token");
				requestBody.Add("client_id", _clientId);
				requestBody.Add("refresh_token", _tokenResult.RefreshTokenId);

				FormUrlEncodedContent formDataContent = new FormUrlEncodedContent(requestBody);
				request.Content = formDataContent;

				await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead)
					.ContinueWith((response) =>
					{
						try
						{
							_tokenResult = ProcessToken(response);
						}
						catch (AggregateException exs)
						{
							Console.WriteLine($"Exceptions: {exs}");
						}
					});
			}
		}

		// 분개장 추가 메서드 샘플
		private static async Task CreateJournalVoucher(Uri baseUri)
		{
			JournalEntry sendObject = new JournalEntry
			{
				ReferenceDate = new DateTime(2017, 7, 7),
				DueDate = new DateTime(2017, 7, 7),
				TaxDate = new DateTime(2017, 7, 7),
				Memo = "분개장 테스트를 위한...",
				Lines = new List<JournalEntryLine>
				{
					new JournalEntryLine
					{
						AccountCode = "81201",
						Debit = 7777,
						Credit = 0,
						ShortName = "81201",
						LineMemo = "from api consumer",
						DueDate = new DateTime(2017, 7, 7),
						ReferenceDate = new DateTime(2017, 7, 7),
						TaxDate = new DateTime(2017, 7, 7),
						BaseSum = 0
					},
					new JournalEntryLine
					{
						AccountCode = "10316",
						Debit = 0,
						Credit = 7777,
						ShortName = "10316",
						LineMemo = "from api consumer",
						DueDate = new DateTime(2017, 7, 7),
						ReferenceDate = new DateTime(2017, 7, 7),
						TaxDate = new DateTime(2017, 7, 7),
						BaseSum = 0
					}
				}
			};

			using (HttpClient client = new HttpClient())
			{
				client.BaseAddress = baseUri;
				HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, string.Concat(baseUri, "api/voucher/add"));

				client.DefaultRequestHeaders.Add("Accept", new MediaTypeHeaderValue("application/json").ToString());
				request.Headers.Add("Authorization", $"Bearer {_tokenResult.AccessToken}");

				request.Content = new StringContent(JsonConvert.SerializeObject( new { Entries = new object[] { sendObject } }), Encoding.UTF8, "application/json");

				await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead)
					.ContinueWith((response) =>
					{
						try
						{
							if (response.Result.IsSuccessStatusCode)
							{
								string getDataMessage = $"{Environment.NewLine}** operation completed @ {DateTime.Now.ToLongDateString()}, {DateTime.Now.ToLongTimeString()}";

								Console.WriteLine($"{getDataMessage} Message: {Environment.NewLine}{JsonConvert.SerializeObject(response.Result.Content.ReadAsAsync<object>().TryResult(), Formatting.Indented)}");
							}
							else
							{
								ProcessFailResponse(response);
							}
						}
						catch (AggregateException exs)
						{
							Console.WriteLine($"Exceptions: {exs}");
						}
					});
			}
		}

		private static async Task SelectJournalVouchersList(Uri baseUri)
		{
			using (HttpClient client = new HttpClient())
			{
				client.BaseAddress = baseUri;
				HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, string.Concat(baseUri, "api/voucher"));

				client.DefaultRequestHeaders.Add("Accept", new MediaTypeHeaderValue("application/json").ToString());
				request.Headers.Add("Authorization", $"Bearer {_tokenResult.AccessToken}");

				await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead)
					.ContinueWith((response) =>
					{
						try
						{
							if (response.Result.IsSuccessStatusCode)
							{
								string getDataMessage = $"{Environment.NewLine}** operation completed @ {DateTime.Now.ToLongDateString()}, {DateTime.Now.ToLongTimeString()}";

								Console.WriteLine($"{getDataMessage} Message: {Environment.NewLine}{JsonConvert.SerializeObject(response.Result.Content.ReadAsAsync<object>().TryResult(), Formatting.Indented)}");
							}
							else
							{
								ProcessFailResponse(response);
							}
						}
						catch (AggregateException exs)
						{
							Console.WriteLine($"Exceptions: {exs}");
						}
					});
			}
		}

		// 정보 조회 메서드 샘플

		#region response process methods

		private static TokenData ProcessToken(Task<HttpResponseMessage> response)
		{
			if (response.Result.IsSuccessStatusCode)
			{
				var responseToken = JsonConvert.SerializeObject(response.Result.Content.ReadAsAsync<object>().TryResult(), Formatting.Indented);

				var token = JObject.Parse(responseToken);

				return new TokenData
				{
					AccessToken = token.GetValue("access_token").ToString(),
					RefreshTokenId = token.GetValue("refresh_token").ToString()
				};
			}
			else
			{
				ProcessFailResponse(response);
				return null;
			}
		}

		private static void ProcessFailResponse(Task<HttpResponseMessage> response)
		{
			Console.WriteLine($"Unsuccessful response message: {Environment.NewLine}HttpStatus: {response.Result.StatusCode}{Environment.NewLine}ReasonPhrase: {response.Result.ReasonPhrase}{Environment.NewLine}Description: {response.Result.Content.ReadAsStringAsync().TryResult()}");
		}

		private static bool IsValid()
		{
			try
			{
				if (_tokenResult != null)
				{
					var result = ParseToken();

					Console.WriteLine(result.ValidFrom.ToLocalTime());
					Console.WriteLine(result.ValidTo.ToLocalTime());

					// expire 체크
					if (DateTime.Now > result.ValidTo.ToLocalTime())
					{
						return false;
					}

					return true;
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex.Message);
			}

			return false;
		}

		private static SecurityToken ParseToken()
		{
			SecurityToken token = null;

			var handler = new JwtSecurityTokenHandler();
			var hmac = new HMACSHA256(TextEncodings.Base64Url.Decode(_clientSecret));
			var signingCredentials = new SigningCredentials(new SymmetricSecurityKey(hmac.Key), SecurityAlgorithms.HmacSha256Signature);
			var signingKey = signingCredentials.Key as SymmetricSecurityKey;

			TokenValidationParameters validationParams = new TokenValidationParameters
			{
				ValidAudience = _clientId,
				ValidIssuer = $"http://{_apiServer}",
				ValidateAudience = true,
				ValidateLifetime = true,
				ValidateIssuer = true,
				ValidateIssuerSigningKey = true,
				IssuerSigningKey = signingKey,
				ClockSkew = TimeSpan.Zero
			};

			handler.ValidateToken(_tokenResult.AccessToken, validationParams, out token);

			return token;
		}

		private static void ExecuteOperation(Action callBack, int sleep = 5)
		{
			callBack.Invoke();
			Thread.Sleep(sleep * 1000);
		}

		#endregion response process methods
	}

	public class TokenData
	{
		public string AccessToken { get; set; }
		public string RefreshTokenId { get; set; }
	}
}