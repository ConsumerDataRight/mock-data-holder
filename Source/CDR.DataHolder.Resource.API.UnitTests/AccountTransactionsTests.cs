using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using CDR.DataHolder.API.Infrastructure.IdPermanence;
using CDR.DataHolder.Domain.Repositories;
using CDR.DataHolder.Resource.API.Business.Models;
using CDR.DataHolder.Resource.API.Business.Responses;
using CDR.DataHolder.Resource.API.Business.Services;
using CDR.DataHolder.Resource.API.Controllers;
using CDR.DataHolder.Resource.API.UnitTests.Fixtures;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace CDR.DataHolder.Resource.API.UnitTests
{
    public class AccountTransactionsTests
    {
        IServiceProvider _serviceProvider;

        public AccountTransactionsTests()
        {
            var seedData = new SeedDataFixture();
            _serviceProvider = seedData.ServiceProvider;
        }

        [Fact]
        public async Task GetTransactions_TimeFilter_Success()
        {
            //Arrange
            var resourceRepository = _serviceProvider.GetRequiredService<IResourceRepository>();
            var statusRepository = _serviceProvider.GetRequiredService<IStatusRepository>();
            var transactionsService = _serviceProvider.GetRequiredService<ITransactionsService>();
            var idPermanenceManager = _serviceProvider.GetRequiredService<IIdPermanenceManager>();
            var loggerFactory = _serviceProvider.GetRequiredService<ILoggerFactory>();
            var logger = loggerFactory.CreateLogger<ResourceController>();

            var privateKey = Guid.NewGuid().ToString();
            string softwareProductId = "c6327f87-687a-4369-99a4-eaacd3bb8210";
            string customerId = "4ee1a8db-13af-44d7-b54b-e94dff3df548";

            var idParameters = new IdPermanenceParameters
            {
                SoftwareProductId = softwareProductId,
                CustomerId = customerId,
            };

            //Generate Account Permanence Id
            var accountId = "1122334455";
            var accountPermanenceId = idPermanenceManager.EncryptId(accountId, idParameters);

            var request = new Mock<HttpRequest>();
            request.Setup(x => x.Scheme).Returns("https");
            request.Setup(x => x.Host).Returns(HostString.FromUriComponent("localhost:8003"));
            request.Setup(x => x.PathBase).Returns(PathString.FromUriComponent($"/cds-au/v1/banking/accounts/{accountPermanenceId}/transactions?oldest-time=2021-04-01T00:00:00Z&newest-time=2021-04-30T23:59:59Z&page=1&page-size=10"));
            request.Setup(x => x.Headers).Returns(new HeaderDictionary() { { "x-v", "1" } });

            var httpContext = Mock.Of<HttpContext>(_ =>
                _.Request == request.Object
            );

            var actionContext = new ActionContext(httpContext, new Microsoft.AspNetCore.Routing.RouteData(), new ControllerActionDescriptor());
            actionContext.HttpContext = httpContext;

            var mockUrlHelper = new Mock<IUrlHelper>();
            mockUrlHelper.Setup(x => x.ActionContext).Returns(actionContext);
            mockUrlHelper.Setup(x => x.RouteUrl(It.IsAny<UrlRouteContext>())).Returns($"cds-au/v1/banking/accounts/{accountPermanenceId}/transactions?oldest-time=2021-04-01T00:00:00Z&newest-time=2021-04-30T23:59:59Z&page=1&page-size=10");

            var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
            {
                new Claim(ClaimTypes.NameIdentifier, customerId),
                new Claim("software_id", softwareProductId),
                new Claim("client_id", Guid.NewGuid().ToString()),
                new Claim("account_id", accountId)
            }, "mock"));

            httpContext.User = user;

            var controllerContext = new ControllerContext(actionContext)
            {
                HttpContext = httpContext
            };

            var controller = new ResourceController(resourceRepository, statusRepository, null, logger, transactionsService, idPermanenceManager);
            controller.ControllerContext = controllerContext;
            controller.Url = mockUrlHelper.Object;

            //Act
            var result = await controller.GetTransactions(new RequestAccountTransactions
            {
                AccountId = accountPermanenceId,
                OldestTime = new DateTime(2021, 4, 01),
                NewestTime = new DateTime(2021, 4, 30),
                Page = "1",
                PageSize = "10"
            }) as OkObjectResult;

            var response = result.Value as ResponseAccountTransactions;

            //Assert
            Assert.Single(response.Data.Transactions);
            Assert.True(IsValid(accountId, new string[] { "TRN12345" }, response.Data, idPermanenceManager, idParameters));
            Assert.Equal(1, response.Meta.TotalRecords);
            Assert.Equal(1, response.Meta.TotalPages);
        }

        [Fact]
        public async Task GetTransactions_AmountFilter_Success()
        {
            //Arrange
            var resourceRepository = _serviceProvider.GetRequiredService<IResourceRepository>();
            var statusRepository = _serviceProvider.GetRequiredService<IStatusRepository>();
            var transactionsService = _serviceProvider.GetRequiredService<ITransactionsService>();
            var idPermanenceManager = _serviceProvider.GetRequiredService<IIdPermanenceManager>();
            var loggerFactory = _serviceProvider.GetRequiredService<ILoggerFactory>();
            var logger = loggerFactory.CreateLogger<ResourceController>();

            //Generate Account Permanence Id
            var idParameters = new IdPermanenceParameters
            {
                SoftwareProductId = "c6327f87-687a-4369-99a4-eaacd3bb8210",
                CustomerId = "4EE1A8DB-13AF-44D7-B54B-E94DFF3DF548",
            };
            var accountId = "1122334455";
            var accountPermanenceId = idPermanenceManager.EncryptId(accountId, idParameters);

            var request = new Mock<HttpRequest>();
            request.Setup(x => x.Scheme).Returns("https");
            request.Setup(x => x.Host).Returns(HostString.FromUriComponent("localhost:8003"));
            request.Setup(x => x.PathBase).Returns(PathString.FromUriComponent($"/cds-au/v1/banking/accounts/{accountPermanenceId}/transactions?oldest-time=2021-04-01T00:00:00Z&newest-time=2021-06-01T00:00:00Z&page=1&page-size=10"));
            request.Setup(x => x.Headers).Returns(new HeaderDictionary() { { "x-v", "1" } });

            var httpContext = Mock.Of<HttpContext>(_ =>
                _.Request == request.Object
            );

            var actionContext = new ActionContext(httpContext, new Microsoft.AspNetCore.Routing.RouteData(), new ControllerActionDescriptor());
            actionContext.HttpContext = httpContext;

            var mockUrlHelper = new Mock<IUrlHelper>();
            mockUrlHelper.Setup(x => x.ActionContext).Returns(actionContext);
            mockUrlHelper.Setup(x => x.RouteUrl(It.IsAny<UrlRouteContext>())).Returns($"cds-au/v1/banking/accounts/{accountPermanenceId}/transactions?oldest-time=2021-04-01T00:00:00Z&newest-time=2021-06-01T00:00:00Z&page=1&page-size=10");

            var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
            {
                new Claim(ClaimTypes.NameIdentifier, "4EE1A8DB-13AF-44D7-B54B-E94DFF3DF548"),
                new Claim("software_id", "c6327f87-687a-4369-99a4-eaacd3bb8210"),
                new Claim("client_id", Guid.NewGuid().ToString()),
                new Claim("account_id", accountId)
            }, "mock"));

            httpContext.User = user;

            var controllerContext = new ControllerContext(actionContext)
            {
                HttpContext = httpContext
            };

            var controller = new ResourceController(resourceRepository, statusRepository, null, logger, transactionsService, idPermanenceManager);
            controller.ControllerContext = controllerContext;
            controller.Url = mockUrlHelper.Object;

            //Act
            var result = await controller.GetTransactions(new RequestAccountTransactions
            {
                AccountId = accountPermanenceId,
                MaxAmount = 320,
                MinAmount = 0,
                OldestTime = new DateTime(2021, 4, 01),
                NewestTime = new DateTime(2021, 6, 01),
                Page = "1",
                PageSize = "10"
            }) as OkObjectResult;

            var response = result.Value as ResponseAccountTransactions;

            //Assert
            Assert.Equal(4, response.Data.Transactions.Length);
            Assert.True(IsValid(accountId, new string[] { "TRN11112", "TRN98765", "TRN11111", "TRN99999" }, response.Data, idPermanenceManager, idParameters));
            Assert.Equal(4, response.Meta.TotalRecords);
            Assert.Equal(1, response.Meta.TotalPages);
            Assert.Equal($"https://localhost:8003/cds-au/v1/banking/accounts/{accountPermanenceId}/transactions?oldest-time=2021-04-01T00:00:00Z&newest-time=2021-06-01T00:00:00Z&page=1&page-size=10", response.Links.Self.ToString());
        }

        private static bool IsValid(
            string validAccountId,
            string[] validTransactionIds,
            AccountTransactionsCollectionModel data, 
            IIdPermanenceManager idPermanenceManager, 
            IdPermanenceParameters idParameters)
        {
            foreach (var item in data.Transactions)
            {
                string acctId = idPermanenceManager.DecryptId(item.AccountId, idParameters);
                if (validAccountId != acctId)
                {
                    return false;
                }

                string txnId = idPermanenceManager.DecryptId(item.TransactionId, idParameters);
                if (!validTransactionIds.Contains(txnId))
                {
                    return false;
                }
            }

            return true;
        }

    }
}
