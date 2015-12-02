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
                    .Where(sod => sod.SalesOrderId == 43659)
                    .ToList()
                    .Select(sod => new {
                        sod.OrderQty,
                        sod.UnitPrice,
                        ExtendedPrice = GetExtendedPrice(sod.OrderQty,  sod.UnitPrice)
                    }).ToList();

                _output.WriteLine(results.ToJson());
            }
        }
        public decimal GetExtendedPrice(short qty, decimal unitPrice) {
            return qty * unitPrice;
        }
    }
}
