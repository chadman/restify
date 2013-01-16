#Restify - .NET REST API Wrapper Pattern#
Restify is an opionated foundation framework to create C# REST API wrappers. It is intended to be used by anyone who wants to create a wrapper for a REST API implementation in C#. (Current only supports OAUTH). Building upon Restify will give you ease in consuming REST APIs.

Assume you have an API that has the following exposed resources:

+ Customer
+ Product
+ Order

For each of these resources, there are the basic actions for each including Get, List, Create, Update, Delete. Before Restify, all that work to create those calls would have to be done manually. By using Restify to inherit, you can now create APISets and Models that will make these calls easy.

First create your Customer entity and decorate it with the attributes that will be used to identify the entity from the API.

		public namespace FakeAPI.Model {
			[XmlRoot("customer")]
			public class Customer {
				[XmlElement("id")]
				public int CustomerID { get; set; }
	
				[XmlElement("firstName")]
				public string FirstName { get; set; }
	
				[XmlElement("lastName")]
				public string LastName { get; set; }
			}
		}

Now create a Restify Set for customers. Think of it like a DbSet in a DataContext. The APISet requires a base domain url and an OAuthTicket. OAuthTickets are explained below.

		public namespace FakeAPI.Sets {
			public class Customers : Restify.ApiSet<FakeAPI.Model.Customer> {
				public Customers(Restify.OAuthTicket ticket, string baseUrl) : base(ticket, baseUrl) { }
	
				// Url for retrieving a specific customer
				protected override string GetUrl {
	            get { return "/Customers/{0}"; }
	
				// Url for creating a new customer
				protected override string CreateUrl {
					get { return "/Customers"; }
				}
	
				// Url for updating a specific customer
				protected override string UpdateUrl {
					get { return "/Customers/{0}"; }
				}
	
				// Not possible to get a list of all customers
				protected override string ListUrl {
	            	get { throw new NotImplementedException(); }
	        	}
			}
		}

By setting the Urls, the APISet now knows where to go to get the data. To make a clean call, lets create a Client that our code can use.

		public namespace FakeAPI {
			public class RestClient : Restify.Client {
				#region Api Sets
				private FakeAPI.Sets.Customers _customers;
	        	public FakeAPI.Sets.Customers Customers {
					get {
						if (_customers == null) {
							_customers == new FakeAPI.Sets.Customers(base.Ticket, base.BaseUrl);
						}
						return _customers;
					}
				}
		        #endregion Api Sets
	
				#region Constructor
				public RestClient(Restify.OAuthTicket ticket) : base (ticket) { }
				#endreigon Constructor
			}
		}
With a client class, calling any method for the resource becomes real easy.
		
		// Instantiate a new client that we created above
		_client = new FakeAPI.RestClient(ticket);

		// Create a new customer
		var customer = new FakeAPI.Model.Customer();
		customer.FirstName = "Rest";
		customer.LastName = "ForLife";

		// Call the api to create a new customer
		customer = _client.Customers.Create(customer);

		// Retrieve a customer
		customer = _client.Customers.Get(123);

		// Update a customer
		customer.LastName = "F0Eva";

		// Call the api to update the customer
		customer = _client.Customers.Update(entity, entity.CustomerID.ToString());

#Authenticating with OAuth#
Authenticating with Restify is super simple. Restify has a static method for Authenticating with First and Second party clients (third party coming soon).

		// Create an oauth ticket that holds the consumer key and secret
		var ticket = new Restify.OAuthTicket {
				ConsumerKey = "3",
				ConsumerSecret = "2d5g4-ddv563-ngh6u67n-sz2323g"
		};

		// Call the API to verify credentials
		ticket = Restify.Client.AuthorizeWithCredentials(ticket, "testuser", "testpass", "https://auth.fakeapi.com/AccessToken");

		// The login was successful if the ticket AccessToken and secret are not null
		if (!string.IsNullOrEmpty(ticket.AccessToken) && !string.IsNullOrEmpty(ticket.AccessTokenSecret)) {
			Consolle.WriteLine("yay");
		}
With your authenticated oauth ticket, you can create a client and pass the authenticated ticket into it which will be used for all API calls.

		
