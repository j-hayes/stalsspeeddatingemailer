# stalsspeeddatingemailer
Parser code for speed dating google form and emailer

Things you must do for this code to work

1) make sure the master list that you add and the responses list match the format in the templates.
2) update the email password in the code. DO NOT CHECK THAT IN TO SOURCE CONTROL
3) the gmail stmp server seems to cut you off at 100ish emails in a minute or something to that effect. So you will likely have to run this more than once

see line 104
foreach (var participant in participants.Where(x=>x.IdNumber >= 0).OrderBy(x=> x.IdNumber)) 
to filter out people who have already recieved their emails when sending the second batch in the second run

you can see who the last successful person is by debugging and catching the exception on line 179 and looking at the variables
    lastParticpantWithoutError = participant;
    lastParticpantWithoutErrorId = participant.Id;
    replace 0 in line 104 with lastParticpantWithoutErrorId
    
   Hopefully we won't need to use this again because we are going to build a website for this next year. 
   
   Cheers!
   Jackson 
