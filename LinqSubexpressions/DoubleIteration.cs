using System.Linq;
using System.Threading;
using LinqSubexpressions.DAL;
using Xunit;
using Xunit.Abstractions;

namespace LinqSubexpressions {
    public class DoubleIteration {
        private readonly ITestOutputHelper _output;

        public DoubleIteration(ITestOutputHelper output) {
            _output = output;
        }
        [Fact]
        public void Iterate_results_twice() {
            using (var db = new MyDbContext()) {
                var results = db.SalesOrderDetails
                    .Select(sod => new {
                        sod.OrderQty,
                        sod.UnitPrice,
                        SlowStatisticCalculation = sod.Sleep100()
                    }).ToList();

                _output.WriteLine(results.ToJson());
            }
        }
    }
    public static class ExpensiveComputations {
        public static SalesOrderDetail Sleep100(this SalesOrderDetail query) {
            Thread.Sleep(100);
            return query;
        }
    }
}
