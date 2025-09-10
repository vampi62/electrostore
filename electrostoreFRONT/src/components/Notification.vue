<template>
	<div ref="notification" class="notification mb-2.5 rounded-lg shadow-sm overflow-hidden cursor-pointer" :class="type" @click="close">
		<div class="sn-progress-loader">
			<span></span>
		</div>
		<div class="flex justify-between items-center p-2.5">
			<div class="flex flex-col justify-between">
				<div class="font-bold mb-2">{{ type }}</div>
				<template v-if="i18n">
					<div class="text-sm" >{{ $t(message) }}</div>
				</template>
				<template v-else>
					<div class="text-sm">{{ message }}</div>
				</template>
			</div>
			<template v-if="type == 'success'">
				<svg xmlns="http://www.w3.org/2000/svg" fill="#4caf50" height="24"
					viewBox="0 0 24 24" width="24">
					<path d="M0 0h24v24H0z" fill="none"></path>
					<path d="M9 16.2L4.8 12l-1.4 1.4L9 19 21 7l-1.4-1.4L9 16.2z"></path>
				</svg>
			</template>
			<template v-if="type == 'info'">
				<svg xmlns="http://www.w3.org/2000/svg" fill="#2196f3" height="24" viewBox="0 0 24 24" width="24">
					<path d="M0 0h24v24H0z" fill="none"/>
					<path d="M12 5.99L19.53 19H4.47L12 5.99M12 2L1 21h22L12 2z"/>
					<path d="M12 16c-.55 0-1-.45-1-1s.45-1 1-1 1 .45 1 1-.45 1-1 1z"/>
					<path d="M12 8c-.55 0-1-.45-1-1s.45-1 1-1 1 .45 1 1-.45 1-1 1z"/>
				</svg>
			</template>
			<template v-if="type == 'error'">
				<svg xmlns="http://www.w3.org/2000/svg" fill="#f44336" height="24" viewBox="0 0 24 24" width="24">
					<path d="M0 0h24v24H0z" fill="none"/>
					<path d="M19 6.41L17.59 5 12 10.59 6.41 5 5 6.41 10.59 12 5 17.59 6.41 19 12 13.41 17.59 19 19 17.59 13.41 12z"/>
				</svg>
			</template>
		</div>
	</div>
</template>

<script>
export default {
	name: "Notification",
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
			}, 400); // 0.4s (anim close)
			this.$refs.notification.classList.add("fade-out");
		},
		startTimer() {
			let duration = 0;
			if (this.type === "info") {
				duration = 7700; // 7.7s (progress)
			} else if (this.type === "success") {
				duration = 7700;
			} else if (this.type === "error") {
				duration = 17700;
			} else {
				duration = 7700;
			}
			this.timer = setTimeout(() => {
				this.close();
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

.fade-out {
	animation: translateX 1s forwards;
}

.notification {
	background-color: #f0f0f0;
	width: 300px;
}

.notification.info {
	border-left: 5px solid #2196f3;
	.sn-progress-loader span {
		background-color: #2196f3;
		display: block;
		height: 100%;
		animation: progress-bar 7.7s linear;
	}
}

.notification.success {
	border-left: 5px solid #4caf50;
	.sn-progress-loader span {
		background-color: #4caf50;
		display: block;
		height: 100%;
		animation: progress-bar 7.7s linear;
	}
}

.notification.error {
	border-left: 5px solid #f44336;
	.sn-progress-loader span {
		background-color: #f44336;
		display: block;
		height: 100%;
		animation: progress-bar 17.7s linear;
	}
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
		transform: translateX(500px);
		opacity: 0;
	}
}
</style>