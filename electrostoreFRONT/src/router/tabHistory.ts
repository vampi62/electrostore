import { createMemoryHistory } from "vue-router";

// vue-router memory history does not keep the previous location like the browser history does (history.state.back),
// this one does, the views use it to know if they can go back in the tab
export function createTabHistory() {
	const history = createMemoryHistory();
	const locations = [] as string[];
	let position = -1;
	const { push, replace, go } = history;
	history.push = (to, data) => {
		locations.splice(position + 1);
		locations.push(to);
		position++;
		push(to, data);
	};
	history.replace = (to, data) => {
		if (position === -1) {
			position = 0;
		}
		locations[position] = to;
		replace(to, data);
	};
	history.go = (delta, shouldTrigger) => {
		position = Math.max(0, Math.min(position + delta, locations.length - 1));
		go.call(history, delta, shouldTrigger);
	};
	Object.defineProperty(history, "state", {
		get: () => ({ back: position > 0 ? locations[position - 1] : null }),
	});
	return history;
}
