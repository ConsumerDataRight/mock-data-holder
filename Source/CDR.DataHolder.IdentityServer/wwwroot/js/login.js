$(document).ready(function () {
	$('#otpToast').css("position", "absolute");
	$('#otpToast').css("top", "0px");
	setTimeout(function () {
		$('#otpToast').toast('show');
	}, 2000);

	// Update the count down every 1 second
	var totalCountdown = 600000; // 10 mins
	var countdownTimer = setInterval(function () {

		totalCountdown -= 1000;

		// Time calculations for days, hours, minutes and seconds
		var minutes = Math.floor((totalCountdown % (1000 * 60 * 60)) / (1000 * 60));
		var seconds = Math.floor((totalCountdown % (1000 * 60)) / 1000);

		// Display the result in the element with id="demo"
		document.getElementById("countdown-timer").innerHTML = ("0" + minutes).slice(-2) + ":" + ("0" + seconds).slice(-2);

		// If the count down is finished, write some text
		if (totalCountdown < 0) {
			clearInterval(countdownTimer);
			document.getElementById("countdown-timer").innerHTML = "EXPIRED";
		}
	}, 1000);
});