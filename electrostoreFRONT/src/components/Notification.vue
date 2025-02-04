<template>
	<div ref="notification" class="notification" :class="type" @click="close">
		<div class="sn-progress-loader">
			<span></span>
		</div>
		<div class="sn-container">
			<div class="sn-texte">
				<div class="sn-title">{{ type }}</div>
				<div class="sn-content" v-if="i18n">{{ $t(message) }}</div>
				<div class="sn-content" v-else>{{ message }}</div>

			</div>
			<div class="icon">
				<svg class="simple-notification-svg" xmlns="http://www.w3.org/2000/svg" fill="#ffffff" height="24"
					viewBox="0 0 24 24" width="24">
					<path d="M0 0h24v24H0z" fill="none"></path>
					<path d="M9 16.2L4.8 12l-1.4 1.4L9 19 21 7l-1.4-1.4L9 16.2z"></path>
				</svg>
			</div>
		</div>
	</div>

</template>

<script>
export default {
	props: {
		message: {
			type: String,
			required: true,
		},
		type: {
			type: String,
			default: "info", // 'info', 'success', 'error', etc.
		},
		i18n: {
			type: Boolean,
			default: false,
		},
		id: {
			type: [String, Number],
			required: true,
		},
	},
	emits: ["remove"],
	data() {
		return {
			timer: null,
		};
	},
	methods: {
		close() {
			if (this.timer) {
				clearTimeout(this.timer);
			}
			this.timer = setTimeout(() => {
				this.$emit("remove", this.id);
			}, 400);
			this.$refs.notification.classList.add("close");
		},
		startTimer() {
			let duration = 0;
			if (this.type === "info") {
				duration = 8100; // 7.7s (progress) + 0.4s (anim)
			} else if (this.type === "success") {
				duration = 8100;
			} else if (this.type === "error") {
				duration = 18100;
			} else {
				duration = 8100;
			}
			this.timer = setTimeout(() => {
				this.$emit("remove", this.id);
			}, duration);
		},
	},
	mounted() {
		this.startTimer();
	},
	beforeUnmount() {
		if (this.timer) {
			clearTimeout(this.timer);
		}
	},
};
</script>
<style scoped>
.notification {
	background: #f0f0f0;
	margin-bottom: 10px;
	border-radius: 5px;
	width: 300px;
	box-shadow: 0px 2px 5px rgba(0, 0, 0, 0.1);
	overflow: hidden;
	cursor: pointer;
}

.notification.info {
	border-left: 5px solid #2196f3;
	animation: translateX 0.4s ease-in-out 7.7s;

	.sn-progress-loader span {
		background-color: #2196f3;
		display: block;
		height: 100%;
		animation: progress-bar 7.7s linear infinite;
	}
}

.notification.success {
	border-left: 5px solid #4caf50;
	animation: translateX 0.4s ease-in-out 7.7s;

	.sn-progress-loader span {
		background-color: #4caf50;
		display: block;
		height: 100%;
		animation: progress-bar 7.7s linear infinite;
	}
}

.notification.error {
	border-left: 5px solid #f44336;
	animation: translateX 0.4s ease-in-out 17.7s;

	.sn-progress-loader span {
		background-color: #f44336;
		display: block;
		height: 100%;
		animation: progress-bar 17.7s linear infinite;
	}
}

.sn-title {
	font-weight: bold;
	margin-bottom: 8px;
}

.sn-content {
	font-size: 14px;
}

.sn-container {
	display: flex;
	justify-content: space-between;
	align-items: center;
	padding: 10px;
}

.sn-texte {
	display: flex;
	flex-direction: column;
	justify-content: space-between;
}

.sn-progress-loader {
	height: 4px;
	background-color: rgba(255, 255, 255, 0.3);
	overflow: hidden;
}

@keyframes progress-bar {
	0% {
		width: 0;
	}

	100% {
		width: 100%;
	}
}

@keyframes translateX {
	0% {
		transform: translateX(0);
		opacity: 1;
	}

	100% {
		transform: translateX(100px);
		opacity: 0;
	}
}
</style>