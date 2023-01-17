using JustScheduler.SampleWeb.Models;
using JustScheduler.SampleWeb.Services;
using Microsoft.AspNetCore.Mvc;

namespace JustScheduler.SampleWeb.Controllers {
    public class HelloController : ControllerBase {
        private readonly IJobTrigger<MessageService, Message> trigger;
        private readonly IJobTrigger<SimpleService> trigger2;

        
        public HelloController(IJobTrigger<MessageService, Message> trigger, IJobTrigger<SimpleService> trigger2) {
            this.trigger = trigger;
            this.trigger2 = trigger2;
        }

        [HttpGet("hello")]
        public string Hello(string message) {
            trigger2.Trigger();
            trigger.Trigger(new Message(message));
            return $"Message sent: {message}";
        }
    }
}