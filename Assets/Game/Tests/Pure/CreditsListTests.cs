using System.Linq;
using NUnit.Framework;

namespace Isekai.Tests
{
    public class CreditsListTests
    {
        [Test]
        public void Every_entry_has_a_title_author_licence_and_group()
        {
            foreach (CreditEntry e in CreditsList.Entries)
            {
                Assert.IsFalse(string.IsNullOrWhiteSpace(e.Title), "title");
                Assert.IsFalse(string.IsNullOrWhiteSpace(e.Author), e.Title + " author");
                Assert.IsFalse(string.IsNullOrWhiteSpace(e.Licence), e.Title + " licence");
                Assert.IsFalse(string.IsNullOrWhiteSpace(e.Group), e.Title + " group");
            }
        }

        [Test]
        public void An_unconfirmed_entry_says_so_in_its_licence_text()
        {
            Assert.IsNotEmpty(CreditsList.Unconfirmed.ToList());
            foreach (CreditEntry e in CreditsList.Unconfirmed)
                StringAssert.StartsWith(CreditsList.UnconfirmedMark, e.Licence, e.Title);
        }

        [Test]
        public void Links_are_web_addresses_or_empty()
        {
            foreach (CreditEntry e in CreditsList.Entries.Where(e => e.Url.Length > 0))
                Assert.IsTrue(e.Url.StartsWith("https://") || e.Url.StartsWith("http://"), e.Title);
        }
    }
}
